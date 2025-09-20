namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Relatório completo do sistema
/// </summary>
public class RelatorioCompletoSistema
{
    public ResumoGeral ResumoGeral { get; set; } = new();
    public List<EstatisticaPapel> EstatisticasPorPapel { get; set; } = new();
    public List<EstatisticaPermissao> EstatisticasPorPermissao { get; set; } = new();
    public List<DistribuicaoCategoria> DistribuicaoCategorias { get; set; } = new();
    public EstatisticasAvancadas? EstatisticasAvancadas { get; set; }
    public ComparacoesSistema? Comparacoes { get; set; }
}