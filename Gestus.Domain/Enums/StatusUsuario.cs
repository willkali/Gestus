namespace Gestus.Domain.Enums;

/// <summary>
/// Define os possíveis status de um usuário no sistema.
/// </summary>
public enum StatusUsuario
{
    /// <summary>
    /// Usuário ativo e pode acessar o sistema normalmente.
    /// </summary>
    Ativo = 1,

    /// <summary>
    /// Usuário inativo, não pode acessar o sistema.
    /// </summary>
    Inativo = 2,

    /// <summary>
    /// Usuário bloqueado por motivos de segurança ou administrativos.
    /// </summary>
    Bloqueado = 3,

    /// <summary>
    /// Usuário pendente de ativação (ex: aguardando confirmação de email).
    /// </summary>
    Pendente = 4
}
