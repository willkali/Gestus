using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gestus.Configuracoes;

/// <summary>
/// Adiciona a documentação do endpoint /connect/token ao Swagger
/// com requestBody application/x-www-form-urlencoded para permitir
/// o Try it out corretamente via Swagger UI.
/// </summary>
public sealed class OpenIddictTokenDocumentFilter : IDocumentFilter
{
    private readonly IConfiguration _configuration;

    public OpenIddictTokenDocumentFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var tokenPath = "/connect/token";
        if (!swaggerDoc.Paths.ContainsKey(tokenPath))
        {
            swaggerDoc.Paths[tokenPath] = new OpenApiPathItem();
        }

        var operation = new OpenApiOperation
        {
            Summary = "Emitir token (OpenID Connect)",
            Description = "Troca credenciais por tokens usando application/x-www-form-urlencoded.",
            Tags = new List<OpenApiTag> { new() { Name = "Autenticação" } }
        };

        // Configurações para pré-preencher client_id/client_secret
        var oidcSection = _configuration.GetSection("OpenIddict");
        var apiClientId = oidcSection.GetValue<string>("Api:ClientId") ?? "gestus_api";
        var apiClientSecret = oidcSection.GetValue<string>("Api:ClientSecret") ?? "gestus_api_secret_2024";
        var prefillSecret = _configuration.GetSection("Swagger").GetValue<bool>("PrefillClientSecret", true);

        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["grant_type"] = new OpenApiSchema
                {
                    Type = "string",
                    Description = "Tipo de grant. Ex.: 'password' ou 'client_credentials'",
                    Example = new OpenApiString("password"),
                    Default = new OpenApiString("password")
                },
                ["scope"] = new OpenApiSchema
                {
                    Type = "string",
                    Description = "Scopes solicitados separados por espaço",
                    Example = new OpenApiString("openid profile email roles"),
                    Default = new OpenApiString("openid profile email roles")
                },
                // Para password flow
                ["username"] = new OpenApiSchema { Type = "string", Description = "Email/usuário (para password)" },
                ["password"] = new OpenApiSchema { Type = "string", Description = "Senha (para password)" },
                // Para authorization_code + PKCE
                ["code"] = new OpenApiSchema { Type = "string", Description = "Authorization code (para authorization_code)" },
                ["redirect_uri"] = new OpenApiSchema { Type = "string", Description = "Redirect URI usado no authorize (authorization_code)" },
                ["code_verifier"] = new OpenApiSchema { Type = "string", Description = "PKCE code_verifier (authorization_code)" },
                // Para refresh_token
                ["refresh_token"] = new OpenApiSchema { Type = "string", Description = "Refresh token (para refresh_token)" }
            },
            Required = new HashSet<string> { "grant_type" }
        };

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/x-www-form-urlencoded"] = new OpenApiMediaType
                {
                    Schema = schema,
                    Example = new OpenApiObject
                    {
                        ["grant_type"] = new OpenApiString("password"),
                        ["username"] = new OpenApiString("willian.cavalcante@skymsen.com"),
                        ["password"] = new OpenApiString(""),
                        ["scope"] = new OpenApiString("openid profile email roles")
                    }
                }
            }
        };

        operation.Responses = new OpenApiResponses
        {
            ["200"] = new OpenApiResponse { Description = "Token gerado com sucesso" },
            ["400"] = new OpenApiResponse { Description = "Requisição inválida (ex.: Content-Type ausente/incorreto)" },
            ["401"] = new OpenApiResponse { Description = "Cliente inválido (verifique client_id/client_secret)" }
        };

        swaggerDoc.Paths[tokenPath].Operations[OperationType.Post] = operation;
    }
}
