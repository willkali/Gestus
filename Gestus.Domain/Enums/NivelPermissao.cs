namespace Gestus.Domain.Enums;

/// <summary>
/// Define os níveis de permissão que podem ser atribuídos.
/// </summary>
public enum NivelPermissao
{
    /// <summary>
    /// Permissão de leitura apenas (visualizar).
    /// </summary>
    Leitura = 1,

    /// <summary>
    /// Permissão de escrita (criar e editar).
    /// </summary>
    Escrita = 2,

    /// <summary>
    /// Permissão de exclusão (deletar).
    /// </summary>
    Exclusao = 3,

    /// <summary>
    /// Permissão administrativa completa (todas as operações).
    /// </summary>
    Administrador = 4
}
