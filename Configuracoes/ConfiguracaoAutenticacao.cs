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

        // ✅ CONFIGURAR AUTHENTICATION COM ESQUEMA PADRÃO
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = configuration["OpenIddict:Server:Issuer"];
            options.Audience = "gestus_api";
            options.RequireHttpsMetadata = false; // Apenas para desenvolvimento
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false, // ✅ Desabilitar para mock tokens
                ValidateAudience = false, // ✅ Desabilitar para mock tokens
                ValidateLifetime = false, // ✅ Desabilitar para mock tokens
                ValidateIssuerSigningKey = false, // ✅ Desabilitar para mock tokens
                ClockSkew = TimeSpan.Zero
            };

            // ✅ CONFIGURAR EVENTOS PARA API (retornar JSON ao invés de redirect)
            options.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    // Não fazer redirect, retornar 401 direto
                    context.HandleResponse();
                    
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        
                        var response = new
                        {
                            erro = "TokenInvalido",
                            mensagem = "Token de acesso inválido ou ausente",
                            statusCode = 401
                        };
                        
                        return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                    }
                    
                    return Task.CompletedTask;
                },
                
                OnForbidden = context =>
                {
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        
                        var response = new
                        {
                            erro = "AcessoNegado",
                            mensagem = "Acesso negado. Permissões insuficientes",
                            statusCode = 403
                        };
                        
                        return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                    }
                    
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}