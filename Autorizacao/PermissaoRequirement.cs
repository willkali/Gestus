using Microsoft.AspNetCore.Authorization;

namespace Gestus.Autorizacao;

/// <summary>
/// Requisito de autorização baseado em permissão (ex.: "Usuarios.Criar").
/// </summary>
public class PermissaoRequirement : IAuthorizationRequirement
{
    public string Permissao { get; }

    public PermissaoRequirement(string permissao)
    {
        Permissao = permissao;
    }
}
