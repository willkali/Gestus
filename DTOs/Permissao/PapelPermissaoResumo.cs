namespace Gestus.DTOs.Permissao;

/// <summary>
/// Resumo de papel associado à permissão
/// </summary>
public class PapelPermissaoResumo
{
    /// <summary>
    /// ID do papel
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nome do papel
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Descrição do papel
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Categoria do papel
    /// </summary>
    public string? Categoria { get; set; }

    /// <summary>
    /// Nível do papel
    /// </summary>
    public int Nivel { get; set; }

    /// <summary>
    /// Data de atribuição da permissão ao papel
    /// </summary>
    public DateTime DataAtribuicao { get; set; }

    /// <summary>
    /// Status ativo/inativo do papel
    /// </summary>
    public bool Ativo { get; set; }
}