using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Gestus.Autorizacao;

/// <summary>
/// Provedor de policies que cria políticas dinamicamente com base no nome da policy.
/// Ex.: Policy "Usuarios.Criar" gera um requisito PermissaoRequirement("Usuarios.Criar").
/// </summary>
public class PermissaoPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissaoPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Cria uma policy dinâmica para qualquer nome fornecido,
        // permitindo uso direto de [Authorize(Policy = "Recurso.Acao")]
        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissaoRequirement(policyName))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
