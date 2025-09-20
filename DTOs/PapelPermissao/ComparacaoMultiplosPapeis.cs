namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Comparação entre múltiplos papéis
/// </summary>
public class ComparacaoMultiplosPapeis
{
    public List<PapelResumoRelatorio> PapeisComparados { get; set; } = new();
    public List<PermissaoDetalhada> PermissoesComuns { get; set; } = new();
    public Dictionary<int, List<PermissaoDetalhada>> PermissoesExclusivas { get; set; } = new();
    public List<PermissaoParcial> PermissoesParciais { get; set; } = new();
    public EstatisticasComparacao Estatisticas { get; set; } = new();
    public DateTime DataComparacao { get; set; }
}