using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Gestus.Configuracoes;

public static class ConfiguracaoSwagger
{
    public static IServiceCollection ConfigurarSwagger(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var swaggerConfig = configuration.GetSection("Swagger");
        
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = swaggerConfig.GetValue<string>("Title", "Gestus API"),
                Version = swaggerConfig.GetValue<string>("Version", "v1"),
                Description = swaggerConfig.GetValue<string>("Description", "Sistema IAM"),
                Contact = new OpenApiContact
                {
                    Name = "Willian Cavalcante",
                    Email = "willian.exercito@gmail.com"
                }
            });

            // JWT Bearer Authorization
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Insira o token JWT no formato: Bearer {seu_token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Incluir comentários XML
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // ✅ REMOVIDO: EnableAnnotations() - não está disponível na versão atual
            // Se precisar de annotations, adicione o pacote Swashbuckle.AspNetCore.Annotations
        });

        return services;
    }
}