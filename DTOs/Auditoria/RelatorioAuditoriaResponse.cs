namespace Gestus.DTOs.Auditoria;

/// <summary>
/// Resposta de relatório de auditoria
/// </summary>
public class RelatorioAuditoriaResponse
{
    public string TipoRelatorio { get; set; } = string.Empty;
    public DateTime DataGeracao { get; set; } = DateTime.UtcNow;
    public string Periodo { get; set; } = string.Empty;
    public int TotalRegistros { get; set; }

    /// <summary>
    /// Dados do relatório
    /// </summary>
    public object Dados { get; set; } = new();

    /// <summary>
    /// Resumo executivo
    /// </summary>
    public ResumoExecutivo Resumo { get; set; } = new();

    /// <summary>
    /// Estatísticas do período
    /// </summary>
    public EstatisticasAuditoria Estatisticas { get; set; } = new();
}