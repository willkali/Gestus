namespace Gestus.DTOs.Permissao;

/// <summary>
/// Informações de categoria de permissões
/// </summary>
public class CategoriaPermissao
{
    /// <summary>
    /// Nome da categoria
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Total de permissões na categoria
    /// </summary>
    public int TotalPermissoes { get; set; }

    /// <summary>
    /// Total de permissões ativas na categoria
    /// </summary>
    public int PermissoesAtivas { get; set; }

    /// <summary>
    /// Total de permissões inativas na categoria
    /// </summary>
    public int PermissoesInativas { get; set; }
}