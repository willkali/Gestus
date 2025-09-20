namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Request para associação em lote
/// </summary>
public class AssociacaoLoteRequest
{
    /// <summary>
    /// Tipo de operação: associar, dissociar, substituir
    /// </summary>
    public string Operacao { get; set; } = string.Empty;

    /// <summary>
    /// Lista de IDs de papéis
    /// </summary>
    public List<int> PapeisIds { get; set; } = new();

    /// <summary>
    /// Lista de IDs de permissões
    /// </summary>
    public List<int> PermissoesIds { get; set; } = new();

    /// <summary>
    /// Observações da operação
    /// </summary>
    public string? Observacoes { get; set; }
}