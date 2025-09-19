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

        // Logging de requisições
        app.UseSerilogRequestLogging();

        // ✅ SWAGGER SEMPRE ATIVO (não apenas em desenvolvimento)
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestus API v1");
            c.RoutePrefix = string.Empty; // Swagger na raiz
            c.DisplayRequestDuration();
            c.EnableTryItOutByDefault();
        });

        // Configurações de desenvolvimento
        if (environment.IsDevelopment())
        {
            // Configurações específicas de desenvolvimento
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

        // Aplicar migrações e seeder (apenas em desenvolvimento)
        if (environment.IsDevelopment())
        {
            await app.AplicarMigracoesESeederAsync();
        }

        return app;
    }

    private static async Task AplicarMigracoesESeederAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GestusDbContexto>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Aplicando migrações do banco de dados...");
            await context.Database.MigrateAsync();
            
            logger.LogInformation("Executando seeder inicial...");
            await ExecutarSeederInicial(scope.ServiceProvider);
            
            logger.LogInformation("Inicialização do banco concluída com sucesso.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro durante a inicialização do banco de dados");
            throw;
        }
    }

    private static async Task ExecutarSeederInicial(IServiceProvider serviceProvider)
    {
        // Implementaremos o seeder mais tarde
        await Task.CompletedTask;
    }
}