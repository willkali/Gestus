using Gestus.Dados;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

namespace Gestus.Configuracoes;

public static class ConfiguracaoHealthChecks
{
    public static IServiceCollection ConfigurarHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddHealthChecks()
            .AddNpgSql(connectionString!, name: "postgresql")
            .AddCheck<DbContextHealthCheck>("gestus_database")
            .AddCheck("self", () => HealthCheckResult.Healthy());

        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(30);
            setup.MaximumHistoryEntriesPerEndpoint(50);
            setup.AddHealthCheckEndpoint("Gestus API", "/saude");
        })
        .AddPostgreSqlStorage(connectionString!, options =>
        {
            // ✅ Corrigido: usar UseNpgsql para configurações específicas do PostgreSQL
            options.UseNpgsql(connectionString!, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("Gestus");
                npgsqlOptions.EnableRetryOnFailure(3);
            });
            
            // Suprimir warnings de migração pendente durante desenvolvimento
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        return services;
    }
}

// Health check personalizado para o DbContext
public class DbContextHealthCheck : IHealthCheck
{
    private readonly GestusDbContexto _context;

    public DbContextHealthCheck(GestusDbContexto context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy("Conexão com PostgreSQL bem-sucedida");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Falha na conexão com PostgreSQL: {ex.Message}");
        }
    }
}