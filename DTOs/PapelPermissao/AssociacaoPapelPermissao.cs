namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Dados de associação entre papel e permissão
/// </summary>
public class AssociacaoPapelPermissao
{
    /// <summary>
    /// ID do papel
    /// </summary>
    public int PapelId { get; set; }

    /// <summary>
    /// Nome do papel
    /// </summary>
    public string PapelNome { get; set; } = string.Empty;

    /// <summary>
    /// ID da permissão
    /// </summary>
    public int PermissaoId { get; set; }

    /// <summary>
    /// Nome da permissão
    /// </summary>
    public string PermissaoNome { get; set; } = string.Empty;

    /// <summary>
    /// Data de atribuição da permissão ao papel
    /// </summary>
    public DateTime DataAtribuicao { get; set; }

    /// <summary>
    /// Status ativo/inativo da associação
    /// </summary>
    public bool Ativo { get; set; }
}