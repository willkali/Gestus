namespace Gestus.DTOs.Permissao;

/// <summary>
/// Request para operações em lote com permissões
/// </summary>
public class OperacaoLotePermissoes
{
    /// <summary>
    /// Tipo de operação: ativar, desativar, excluir, categoria-alterar
    /// </summary>
    public string TipoOperacao { get; set; } = string.Empty;

    /// <summary>
    /// IDs das permissões para a operação
    /// </summary>
    public List<int> PermissoesIds { get; set; } = new();

    /// <summary>
    /// Parâmetros específicos da operação
    /// </summary>
    public Dictionary<string, string> Parametros { get; set; } = new();

    /// <summary>
    /// Observações da operação
    /// </summary>
    public string? Observacoes { get; set; }
}