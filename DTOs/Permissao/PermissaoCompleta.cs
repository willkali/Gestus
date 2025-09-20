namespace Gestus.DTOs.Permissao;

/// <summary>
/// Dados completos de uma permissão com relacionamentos
/// </summary>
public class PermissaoCompleta
{
    /// <summary>
    /// ID único da permissão
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nome da permissão
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Descrição da permissão
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Recurso controlado pela permissão
    /// </summary>
    public string Recurso { get; set; } = string.Empty;

    /// <summary>
    /// Ação permitida pela permissão
    /// </summary>
    public string Acao { get; set; } = string.Empty;

    /// <summary>
    /// Categoria da permissão
    /// </summary>
    public string? Categoria { get; set; }

    /// <summary>
    /// Status ativo/inativo
    /// </summary>
    public bool Ativo { get; set; }

    /// <summary>
    /// Data de criação da permissão
    /// </summary>
    public DateTime DataCriacao { get; set; }

    /// <summary>
    /// Total de papéis que possuem esta permissão
    /// </summary>
    public int TotalPapeis { get; set; }

    /// <summary>
    /// Lista de papéis que possuem esta permissão
    /// </summary>
    public List<PapelPermissaoResumo> Papeis { get; set; } = new();

    /// <summary>
    /// Estatísticas de uso da permissão
    /// </summary>
    public EstatisticasPermissao Estatisticas { get; set; } = new();
}