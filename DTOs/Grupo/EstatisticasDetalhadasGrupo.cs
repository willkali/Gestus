namespace Gestus.DTOs.Grupo;

/// <summary>
/// Estatísticas detalhadas de um grupo específico
/// </summary>
public class EstatisticasDetalhadasGrupo
{
    /// <summary>
    /// Total de membros no grupo
    /// </summary>
    public int TotalMembros { get; set; }

    /// <summary>
    /// Membros ativos
    /// </summary>
    public int MembrosAtivos { get; set; }

    /// <summary>
    /// Membros inativos
    /// </summary>
    public int MembrosInativos { get; set; }

    /// <summary>
    /// Novos membros no período
    /// </summary>
    public int NovosMembros { get; set; }

    /// <summary>
    /// Taxa de crescimento percentual
    /// </summary>
    public decimal TaxaCrescimento { get; set; }

    /// <summary>
    /// Distribuição de membros por papel
    /// </summary>
    public Dictionary<string, int> DistribuicaoPorPapel { get; set; } = new();

    /// <summary>
    /// Período da análise
    /// </summary>
    public string Periodo { get; set; } = string.Empty;

    /// <summary>
    /// Data da última adesão
    /// </summary>
    public DateTime? UltimaAdesao { get; set; }

    /// <summary>
    /// Data da primeira adesão
    /// </summary>
    public DateTime? PrimeiraAdesao { get; set; }

    /// <summary>
    /// Média de adesões por mês
    /// </summary>
    public decimal MediaAdesoesPorMes { get; set; }

    /// <summary>
    /// Tendência de crescimento
    /// </summary>
    public string TendenciaCrescimento { get; set; } = "Estável";
}