using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Gestus.Extensoes;

/// <summary>
/// Extensões para autorização baseada em permissões
/// </summary>
public static class AutorizacaoExtensions
{
    /// <summary>
    /// Verifica se o usuário tem uma permissão específica
    /// </summary>
    public static bool TemPermissao(this ClaimsPrincipal user, string recurso, string acao)
    {
        // ✅ SuperAdmin tem todas as permissões
        if (user.IsInRole("SuperAdmin"))
        {
            return true;
        }

        // ✅ Verificar por permissão específica no formato "Recurso.Acao"
        var permissao = $"{recurso}.{acao}";
        return user.HasClaim("permissao", permissao);
    }

    /// <summary>
    /// Verifica se o usuário tem qualquer uma das permissões especificadas
    /// </summary>
    public static bool TemQualquerPermissao(this ClaimsPrincipal user, params string[] permissoes)
    {
        if (user.IsInRole("SuperAdmin"))
        {
            return true;
        }

        return permissoes.Any(p => user.HasClaim("permissao", p));
    }
}

/// <summary>
/// Atributo para autorização baseada em permissões
/// </summary>
public class PermissaoAttribute : AuthorizeAttribute
{
    public PermissaoAttribute(string recurso, string acao)
    {
        Policy = $"{recurso}.{acao}";
    }
}