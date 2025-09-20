namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Resposta de operação em lote
/// </summary>
public class RespostaAssociacaoLote
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Total de associações processadas
    /// </summary>
    public int TotalProcessadas { get; set; }

    /// <summary>
    /// Total de sucessos
    /// </summary>
    public int TotalSucessos { get; set; }

    /// <summary>
    /// Total de falhas
    /// </summary>
    public int TotalFalhas { get; set; }

    /// <summary>
    /// Detalhes das operações
    /// </summary>
    public List<DetalheOperacao> Detalhes { get; set; } = new();

    /// <summary>
    /// Duração da operação
    /// </summary>
    public TimeSpan Duracao { get; set; }
}