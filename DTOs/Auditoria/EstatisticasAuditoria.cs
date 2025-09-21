namespace Gestus.DTOs.Auditoria;

/// <summary>
/// Estatísticas gerais de auditoria
/// </summary>
public class EstatisticasAuditoria
{
    /// <summary>
    /// Total de registros no período
    /// </summary>
    public int TotalRegistros { get; set; }

    /// <summary>
    /// Registros por dia no período
    /// </summary>
    public Dictionary<DateTime, int> RegistrosPorDia { get; set; } = new();

    /// <summary>
    /// Ações mais frequentes
    /// </summary>
    public Dictionary<string, int> AcoesMaisFrequentes { get; set; } = new();

    /// <summary>
    /// Recursos mais acessados
    /// </summary>
    public Dictionary<string, int> RecursosMaisAcessados { get; set; } = new();

    /// <summary>
    /// Usuários mais ativos
    /// </summary>
    public List<UsuarioAtivo> UsuariosMaisAtivos { get; set; } = new();

    /// <summary>
    /// Horários de maior atividade
    /// </summary>
    public Dictionary<int, int> AtividadePorHora { get; set; } = new();

    /// <summary>
    /// IPs mais frequentes
    /// </summary>
    public Dictionary<string, int> IpsMaisFrequentes { get; set; } = new();

    /// <summary>
    /// Período da análise
    /// </summary>
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }

    /// <summary>
    /// Média de operações por dia
    /// </summary>
    public decimal MediaOperacoesPorDia { get; set; }

    /// <summary>
    /// Tendência de atividade
    /// </summary>
    public string TendenciaAtividade { get; set; } = "Estável";
}