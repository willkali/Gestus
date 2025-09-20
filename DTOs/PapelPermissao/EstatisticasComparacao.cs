namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Estatísticas de comparação
/// </summary>
public class EstatisticasComparacao
{
    public int TotalPermissoesComuns { get; set; }
    public int TotalPermissoesExclusivas { get; set; }
    public int TotalPermissoesParciais { get; set; }
    public string PapelComMaisPermissoes { get; set; } = string.Empty;
    public string PapelComMenosPermissoes { get; set; } = string.Empty;
}