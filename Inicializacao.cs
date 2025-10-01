using Gestus.Configuracoes;
using Gestus.Dados;
using Serilog;
using Prometheus;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using System.Reflection;
using Gestus.Services;
using System.Text;
using System.Net.Http;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Gestus.Converters;
using System.Text.Json;

namespace Gestus;

public static class Inicializacao
{
    public static void ConfigurarServicos(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;

        // Configurar Serilog
        builder.Host.UseSerilog((context, loggerConfiguration) =>
        {
            loggerConfiguration.ReadFrom.Configuration(context.Configuration);
        });

        // Entity Framework e Identity
        services.AdicionarEntityFramework(configuration);

        // ADICIONAR: Serviços personalizados
        services.AddSingleton<ITimezoneService, TimezoneService>();
        services.AddScoped<IArquivoService, ArquivoService>();
        services.AddScoped<UsuarioLoginService>();

        // SERVIÇOS DE CRIPTOGRAFIA E EMAIL
        services.AddScoped<IChaveVersaoService, ChaveVersaoService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITemplateService, TemplateService>();

        // Controllers e API
        services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // ✅ REGISTRAR CONVERSORES PERSONALIZADOS
                    options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
                    options.JsonSerializerOptions.Converters.Add(new NullableDateTimeJsonConverter());
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

        services.AddEndpointsApiExplorer();
        services.ConfigurarSwagger(configuration);

        // CORS
        services.ConfigurarCors(configuration);

        // Autenticação e Autorização
        services.ConfigurarAutenticacao(configuration);

        // Autorização baseada em permissões (bypass para SuperAdmin e curinga "*")
        services.AddAuthorization();
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Gestus.Autorizacao.PermissaoHandler>();
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, Gestus.Autorizacao.PermissaoPolicyProvider>();

        // Health Checks
        services.ConfigurarHealthChecks(configuration);

        // AutoMapper (corrigido)
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // FluentValidation (corrigido)
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // HttpClient com Resilience (nova abordagem)
        services.AddHttpClient("default")
                .AddStandardResilienceHandler();

        services.AddHttpClient();
    }

    public static async Task<WebApplication> ConfigurarPipelineAsync(this WebApplication app)
    {
        var environment = app.Environment;

        // ✅ Log do ambiente (manter para debug)
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation($"🔍 Ambiente atual: {environment.EnvironmentName}");
        logger.LogInformation($"🔍 É Development? {environment.IsDevelopment()}");

        // Logging de requisições
        app.UseSerilogRequestLogging();

        // ✅ SWAGGER SEMPRE ATIVO
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestus API v1");
            c.RoutePrefix = string.Empty;
            c.DisplayRequestDuration();
            c.EnableTryItOutByDefault();
        });

        // ✅ MUDANÇA: EXECUTAR SEEDER EM QUALQUER AMBIENTE
        await app.AplicarMigracoesESeederAsync();

        if (!environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        // HTTPS redirection controlado por configuração (padrão: desativado)
        var enforceHttps = app.Configuration.GetValue<bool>("Security:EnforceHttps", false);
        if (enforceHttps)
        {
            app.UseHttpsRedirection();
        }

        // Arquivos estáticos somente se a pasta existir
        var frontEndPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");

        if (Directory.Exists(frontEndPath))
        {
            app.UseDefaultFiles(); // Para servir index.html automaticamente
            app.UseStaticFiles();

            // Fallback para SPA - todas as rotas não-API devem retornar index.html
            app.MapFallbackToFile("index.html");

            logger.LogInformation("✅ Servindo arquivos estáticos do front-end de: {FrontEndPath}", frontEndPath);
        }
        else
        {
            logger.LogWarning("⚠️ Pasta do front-end não encontrada em: {FrontEndPath}", frontEndPath);
        }

        // CORS
        app.UseCors("PoliticaCorsGestus");

        // Dev helper para erro de Content-Type no /connect/token
        if (environment.IsDevelopment())
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.Equals("/connect/token", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrEmpty(context.Request.ContentType))
                {
                    context.Response.StatusCode = 400;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"invalid_request\",\"error_description\":\"Missing Content-Type. Use 'application/x-www-form-urlencoded' and URL-encoded body.\",\"error_uri\":\"https://documentation.openiddict.com/errors/ID2081\"}");
                    return;
                }
                await next();
            });
        }

        // Injeção de client_id/client_secret padrão (controlado por configuração)
        if (app.Configuration.GetSection("Security:DefaultClient").GetValue<bool>("Enabled", true))
        {
            var defaultClientId = app.Configuration.GetSection("Security:DefaultClient").GetValue<string>("ClientId") ?? "gestus_api";
            var defaultClientSecret = app.Configuration.GetSection("Security:DefaultClient").GetValue<string>("ClientSecret") ?? "gestus_api_secret_2024";

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.Equals("/connect/token", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase)
                    && context.Request.HasFormContentType)
                {
                    // Permite reler o corpo depois
                    context.Request.EnableBuffering();

                    var form = await context.Request.ReadFormAsync();
                    var dict = new Dictionary<string, StringValues>(StringComparer.Ordinal);
                    foreach (var kv in form)
                    {
                        dict[kv.Key] = kv.Value;
                    }

                    var changed = false;
                    if (!dict.TryGetValue("client_id", out var cid) || StringValues.IsNullOrEmpty(cid))
                    {
                        dict["client_id"] = new StringValues(defaultClientId);
                        changed = true;
                    }
                    if (!dict.TryGetValue("client_secret", out var csec) || StringValues.IsNullOrEmpty(csec))
                    {
                        dict["client_secret"] = new StringValues(defaultClientSecret);
                        changed = true;
                    }

                    if (changed)
                    {
                        // Reescreve o corpo como x-www-form-urlencoded com os campos injetados
                        var payload = string.Join("&", dict.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value.ToString())}"));
                        var bytes = Encoding.UTF8.GetBytes(payload);
                        context.Request.Body = new MemoryStream(bytes);
                        context.Request.ContentLength = bytes.Length;
                        context.Request.ContentType = "application/x-www-form-urlencoded";

                        // Reseta o parser para que próximos componentes releiam o form do novo corpo
                        context.Features.Set<IFormFeature>(new FormFeature(context.Request));
                    }

                    // Garante que a posição volte ao início para quem vier depois
                    if (context.Request.Body.CanSeek)
                        context.Request.Body.Position = 0;
                }

                await next();
            });
        }

        // Autenticação e Autorização
        app.UseAuthentication();
        app.UseAuthorization();

        // Métricas do Prometheus
        app.UseMetricServer();
        app.UseHttpMetrics();

        // Health Checks
        app.UseHealthChecks("/saude", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(x => new
                    {
                        name = x.Key,
                        status = x.Value.Status.ToString(),
                        exception = x.Value.Exception?.Message,
                        duration = x.Value.Duration.ToString()
                    })
                };
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
            }
        });
        app.UseHealthChecksUI(options => options.UIPath = "/saude-ui");

        // Controllers
        app.MapControllers();

        return app;
    }

    private static async Task AplicarMigracoesESeederAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GestusDbContexto>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("🔧 Verificando estado do banco de dados...");

            // ✅ Verificar se pode conectar
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogError("❌ Não foi possível conectar ao banco. Pulando inicialização.");
                return;
            }

            // ✅ Tentar aplicar migrações, mas não falhar se tabelas já existirem
            try
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("✅ Migrações aplicadas com sucesso");
            }
            catch (Exception migrationEx)
            {
                logger.LogWarning($"⚠️ Erro ao aplicar migrações (provavelmente já existem): {migrationEx.Message}");
                logger.LogInformation("🔄 Continuando com o seeder mesmo assim...");
            }

            logger.LogInformation("🌱 Executando seeder inicial...");
            await SeederInicial.ExecutarAsync(scope.ServiceProvider);

            // ✅ ADICIONAR: Inicializar templates padrão
            logger.LogInformation("📧 Inicializando templates de email padrão...");
            var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
            await templateService.InicializarTemplatesPadraoAsync();
            logger.LogInformation("✅ Templates de email inicializados com sucesso");

            logger.LogInformation("✅ Inicialização do banco concluída com sucesso.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Erro durante a inicialização do banco de dados");
            // ✅ NÃO fazer throw para não parar a aplicação
            logger.LogWarning("⚠️ Continuando inicialização da aplicação mesmo com erro no seeder...");
        }
    }
}