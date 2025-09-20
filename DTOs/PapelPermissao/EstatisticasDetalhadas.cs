namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Estatísticas detalhadas
/// </summary>
public class EstatisticasDetalhadas
{
    public string Periodo { get; set; } = string.Empty;
    public DateTime? DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public EstatisticasGeraisPeriodo EstatisticasGerais { get; set; } = new();
    public TendenciasTempo Tendencias { get; set; } = new();
    public Distribuicoes Distribuicoes { get; set; } = new();
    public Rankings Rankings { get; set; } = new();
    public DateTime DataGeracao { get; set; }
}