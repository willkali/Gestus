using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Gestus.Autorizacao;

/// <summary>
/// Handler que concede acesso quando:
/// - Usuário é SuperAdmin (bypass total); ou
/// - Possui claim de permissão coringa "*"; ou
/// - Possui a permissão exigida pela policy.
/// </summary>
public class PermissaoHandler : AuthorizationHandler<PermissaoRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissaoRequirement requirement)
    {
        // Bypass total para SuperAdmin
        if (context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Curinga de permissões
        var permissoes = context.User.FindAll("permissao").Select(c => c.Value);
        if (permissoes.Contains("*") || permissoes.Contains(requirement.Permissao))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
