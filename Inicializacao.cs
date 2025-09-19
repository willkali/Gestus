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

        // ✅ ADICIONAR: Serviço de timezone
        services.AddScoped<ITimezoneService, TimezoneService>();

        // Controllers e API
        services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
                });

        // OpenAPI/Swagger
        services.AddEndpointsApiExplorer();
        services.ConfigurarSwagger(configuration);

        // CORS
        services.ConfigurarCors(configuration);

        // Autenticação e Autorização
        services.ConfigurarAutenticacao(configuration);

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

        if (environment.IsDevelopment())
        {
            // ✅ SEEDER APENAS EM DESENVOLVIMENTO (restaurado)
            await app.AplicarMigracoesESeederAsync();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        // HTTPS Redirection
        app.UseHttpsRedirection();

        // Arquivos estáticos
        app.UseStaticFiles();

        // CORS
        app.UseCors("PoliticaCorsGestus");

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