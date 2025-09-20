using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text;

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
                Description = swaggerConfig.GetValue<string>("Description", "Sistema IAM Gestus - Gerenciamento de Identidade e Acesso"),
                Contact = new OpenApiContact
                {
                    Name = "Willian Cavalcante",
                    Email = "willian.exercito@gmail.com"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // ✅ CONFIGURAÇÃO CORRETA DO JWT BEARER
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = @"JWT Authorization header usando o esquema Bearer.
                              Entre com 'Bearer' [espaço] e então o token.
                              Exemplo: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9'"
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
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header
                    },
                    Array.Empty<string>()
                }
            });

            // ✅ SISTEMA APRIMORADO DE SCHEMA IDs - RESOLVER CONFLITOS COM GENÉRICOS
            options.CustomSchemaIds(type => GerarSchemaIdUnico(type));

            // ✅ CONFIGURAÇÕES ADICIONAIS PARA MELHOR DOCUMENTAÇÃO
            options.DescribeAllParametersInCamelCase();

            // ✅ FILTROS PERSONALIZADOS PARA MELHORAR A DOCUMENTAÇÃO
            options.OperationFilter<AuthorizeCheckOperationFilter>();

            // Incluir comentários XML
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // ✅ CONFIGURAR EXEMPLOS DE RESPOSTA
            options.MapType<DateTime>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "date-time",
                Example = OpenApiAnyFactory.CreateFromJson("\"2024-01-01T12:00:00Z\"")
            });
        });

        return services;
    }

    /// <summary>
    /// Gera Schema ID único para tipos, incluindo genéricos complexos
    /// </summary>
    /// <param name="type">Tipo para gerar o Schema ID</param>
    /// <returns>Schema ID único</returns>
    private static string GerarSchemaIdUnico(Type type)
    {
        try
        {
            // ✅ CASO 1: Tipos não genéricos simples
            if (!type.IsGenericType && !type.IsNested)
            {
                return LimparNomeClasse(type.Name);
            }

            // ✅ CASO 2: Tipos genéricos (ex: RespostaPaginada<T>, List<T>)
            if (type.IsGenericType)
            {
                return GerarSchemaIdGenerico(type);
            }

            // ✅ CASO 3: Tipos aninhados (nested classes)
            if (type.IsNested)
            {
                return GerarSchemaIdAninhado(type);
            }

            // ✅ FALLBACK: Usar nome completo limpo
            return LimparNomeCompleto(type.FullName ?? type.Name);
        }
        catch (Exception)
        {
            // ✅ FALLBACK FINAL: Nome da classe + hash do assembly
            return $"{type.Name}_{type.Assembly.GetHashCode():X8}";
        }
    }

    /// <summary>
    /// Gera Schema ID para tipos genéricos
    /// </summary>
    private static string GerarSchemaIdGenerico(Type type)
    {
        var nomeBase = type.Name;
        
        // Remover backtick e número de parâmetros genéricos (ex: RespostaPaginada`1 -> RespostaPaginada)
        if (nomeBase.Contains('`'))
        {
            nomeBase = nomeBase.Substring(0, nomeBase.IndexOf('`'));
        }

        // Obter argumentos genéricos
        var argumentosGenericos = type.GetGenericArguments();
        
        if (argumentosGenericos.Length == 0)
        {
            return LimparNomeClasse(nomeBase);
        }

        // Construir nome com argumentos genéricos
        var sb = new StringBuilder();
        sb.Append(LimparNomeClasse(nomeBase));
        sb.Append("Of");

        for (int i = 0; i < argumentosGenericos.Length; i++)
        {
            if (i > 0) sb.Append("And");
            
            var argumento = argumentosGenericos[i];
            
            // Recursão para argumentos genéricos aninhados
            if (argumento.IsGenericType)
            {
                sb.Append(GerarSchemaIdGenerico(argumento));
            }
            else
            {
                sb.Append(LimparNomeClasse(argumento.Name));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gera Schema ID para tipos aninhados
    /// </summary>
    private static string GerarSchemaIdAninhado(Type type)
    {
        var nomes = new List<string>();
        var tipoAtual = type;

        // Percorrer hierarquia de tipos aninhados
        while (tipoAtual != null)
        {
            nomes.Insert(0, LimparNomeClasse(tipoAtual.Name));
            tipoAtual = tipoAtual.DeclaringType;
        }

        return string.Join(".", nomes);
    }

    /// <summary>
    /// Limpa nome da classe removendo caracteres especiais
    /// </summary>
    private static string LimparNomeClasse(string nome)
    {
        if (string.IsNullOrEmpty(nome))
            return "Unknown";

        // Remover caracteres especiais
        var sb = new StringBuilder();
        foreach (char c in nome)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
        }

        var resultado = sb.ToString();
        return string.IsNullOrEmpty(resultado) ? "Unknown" : resultado;
    }

    /// <summary>
    /// Limpa nome completo do tipo
    /// </summary>
    private static string LimparNomeCompleto(string nomeCompleto)
    {
        if (string.IsNullOrEmpty(nomeCompleto))
            return "Unknown";

        // Remover namespace comum
        var nome = nomeCompleto
            .Replace("Gestus.DTOs.", "")
            .Replace("Gestus.Controllers.", "")
            .Replace("Gestus.Modelos.", "")
            .Replace("System.Collections.Generic.", "")
            .Replace("+", ".");

        // Limitar tamanho
        if (nome.Length > 100)
        {
            var partes = nome.Split('.');
            if (partes.Length > 2)
            {
                nome = string.Join(".", partes.Skip(partes.Length - 2));
            }
        }

        return LimparNomeClasse(nome);
    }
}

/// <summary>
/// Filtro para adicionar informações de autorização nas operações
/// </summary>
public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Verificar se a ação ou controller tem atributo Authorize
        var hasAuthorize = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Any() ?? false;

        // Verificar se tem AllowAnonymous
        var hasAllowAnonymous = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>()
            .Any() ?? false;

        if (hasAuthorize && !hasAllowAnonymous)
        {
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Não autorizado - Token inválido ou ausente" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Proibido - Permissões insuficientes" });

            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        }
                    ] = Array.Empty<string>()
                }
            };
        }
    }
}

/// <summary>
/// Factory para criar OpenApiAny a partir de JSON
/// </summary>
public static class OpenApiAnyFactory
{
    public static Microsoft.OpenApi.Any.IOpenApiAny CreateFromJson(string json)
    {
        try
        {
            // Simples implementação para datas
            if (json.Contains("2024-01-01"))
            {
                return new Microsoft.OpenApi.Any.OpenApiString(json.Trim('"'));
            }
            
            return new Microsoft.OpenApi.Any.OpenApiString(json);
        }
        catch
        {
            return new Microsoft.OpenApi.Any.OpenApiString(json);
        }
    }
}