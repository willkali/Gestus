using Gestus.Dados;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
                       .AllowRefreshTokenFlow();

                // Configurar chaves de desenvolvimento (apenas para desenvolvimento)
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                // Configurar tempos de vida dos tokens
                var accessTokenLifetime = serverConfig.GetValue<string>("AccessTokenLifetime");
                if (TimeSpan.TryParse(accessTokenLifetime, out var accessLifetime))
                {
                    options.SetAccessTokenLifetime(accessLifetime);
                }

                var refreshTokenLifetime = serverConfig.GetValue<string>("RefreshTokenLifetime");
                if (TimeSpan.TryParse(refreshTokenLifetime, out var refreshLifetime))
                {
                    options.SetRefreshTokenLifetime(refreshLifetime);
                }

                // Usar ASP.NET Core hosting
                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough()
                       .EnableAuthorizationEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        // Configurar JWT Bearer Authentication como fallback
        services.AddAuthentication()
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["OpenIddict:Server:Issuer"];
                options.Audience = "gestus_api";
                options.RequireHttpsMetadata = false; // Apenas para desenvolvimento
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        return services;
    }
}