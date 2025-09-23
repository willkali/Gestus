using Gestus.Dados;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using OpenIddict.Validation.AspNetCore;

namespace Gestus.Configuracoes;

public static class ConfiguracaoAutenticacao
{
    public static IServiceCollection ConfigurarAutenticacao(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configurar OpenIddict
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<GestusDbContexto>();
            })
            .AddServer(options =>
            {
                var serverConfig = configuration.GetSection("OpenIddict:Server");
                
                // Configurar endpoints
                options.SetTokenEndpointUris("/connect/token")
                       .SetAuthorizationEndpointUris("/connect/authorize")
                       .SetIntrospectionEndpointUris("/connect/introspect")
                       .SetRevocationEndpointUris("/connect/revoke");

                // Configurar fluxos permitidos
                options.AllowClientCredentialsFlow()
                       .AllowAuthorizationCodeFlow()
                       .AllowRefreshTokenFlow()
                       .AllowPasswordFlow();

                // ✅ CERTIFICADOS PARA DESENVOLVIMENTO
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                // ✅ PERMITIR HTTP EM DESENVOLVIMENTO
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    options.DisableAccessTokenEncryption();
                }

                // ✅ CONFIGURAR TEMPOS
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(60));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));

                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough()
                       .EnableAuthorizationEndpointPassthrough()
                       .DisableTransportSecurityRequirement();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        // ✅ USAR APENAS OPENIDDICT VALIDATION (remover JWT Bearer)
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        return services;
    }
}