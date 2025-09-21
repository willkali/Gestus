namespace Gestus.DTOs.Auditoria;

/// <summary>
/// Request para geração de relatórios de auditoria
/// </summary>
public class RelatorioAuditoriaRequest
{
    /// <summary>
    /// Tipo de relatório: atividade-usuario, historico-recurso, timeline, compliance
    /// </summary>
    public string TipoRelatorio { get; set; } = string.Empty;

    /// <summary>
    /// Período do relatório
    /// </summary>
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }

    /// <summary>
    /// Filtros específicos
    /// </summary>
    public FiltrosAuditoria? Filtros { get; set; }

    /// <summary>
    /// Formato de saída: json, csv, excel, pdf
    /// </summary>
    public string Formato { get; set; } = "json";

    /// <summary>
    /// Incluir gráficos (para PDF)
    /// </summary>
    public bool IncluirGraficos { get; set; } = false;

    /// <summary>
    /// Agrupar por (usuário, recurso, ação, data)
    /// </summary>
    public string? AgruparPor { get; set; }

    /// <summary>
    /// Incluir detalhes de alterações
    /// </summary>
    public bool IncluirDetalhesAlteracoes { get; set; } = true;
}