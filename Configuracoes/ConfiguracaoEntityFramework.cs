using Gestus.Dados;
using Gestus.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gestus.Configuracoes;

public static class ConfiguracaoEntityFramework
{
    public static IServiceCollection AdicionarEntityFramework(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Configurar DbContext com PostgreSQL
        services.AddDbContext<GestusDbContexto>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("Gestus");
                
                // ✅ CORRIGIDO: Configurar retry strategy adequadamente
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                
                // ✅ ADICIONAR: Configurar timeout
                npgsqlOptions.CommandTimeout(30);
            });

            // ✅ ADICIONAR: Configurar warnings
            options.ConfigureWarnings(warnings =>
            {
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning);
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
            });

            // ✅ REMOVIDO: UseQuerySplittingBehavior não existe
            // O query splitting será configurado no contexto quando necessário

            // Configurações de desenvolvimento
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            // Integração com OpenIddict
            options.UseOpenIddict();
        });

        // Configurar ASP.NET Core Identity
        services.AddIdentity<Usuario, Papel>(options =>
        {
            // Configurações de senha
            var passwordConfig = configuration.GetSection("Identity:Password");
            options.Password.RequiredLength = passwordConfig.GetValue<int>("RequiredLength", 8);
            options.Password.RequireDigit = passwordConfig.GetValue<bool>("RequireDigit", true);
            options.Password.RequireLowercase = passwordConfig.GetValue<bool>("RequireLowercase", true);
            options.Password.RequireUppercase = passwordConfig.GetValue<bool>("RequireUppercase", true);
            options.Password.RequireNonAlphanumeric = passwordConfig.GetValue<bool>("RequireNonAlphanumeric", true);

            // Configurações de bloqueio
            var lockoutConfig = configuration.GetSection("Identity:Lockout");
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.Parse(
                lockoutConfig.GetValue<string>("DefaultLockoutTimeSpan", "00:05:00"));
            options.Lockout.MaxFailedAccessAttempts = lockoutConfig.GetValue<int>("MaxFailedAccessAttempts", 5);
            options.Lockout.AllowedForNewUsers = lockoutConfig.GetValue<bool>("AllowedForNewUsers", true);

            // Configurações de login
            var signInConfig = configuration.GetSection("Identity:SignIn");
            options.SignIn.RequireConfirmedEmail = signInConfig.GetValue<bool>("RequireConfirmedEmail", false);
            options.SignIn.RequireConfirmedPhoneNumber = signInConfig.GetValue<bool>("RequireConfirmedPhoneNumber", false);

            // Configurações de usuário
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        })
        .AddEntityFrameworkStores<GestusDbContexto>()
        .AddDefaultTokenProviders();

        return services;
    }
}