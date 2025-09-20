namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Permissão com detalhes de atribuição
/// </summary>
public class PermissaoDetalhada
{
    /// <summary>
    /// ID da permissão
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nome da permissão
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Categoria da permissão
    /// </summary>
    public string? Categoria { get; set; }

    /// <summary>
    /// Data de atribuição
    /// </summary>
    public DateTime DataAtribuicao { get; set; }

    /// <summary>
    /// Status ativo/inativo
    /// </summary>
    public bool Ativo { get; set; }

    /// <summary>
    /// Quantos outros papéis têm esta permissão
    /// </summary>
    public int OutrosPapeisComPermissao { get; set; }
}