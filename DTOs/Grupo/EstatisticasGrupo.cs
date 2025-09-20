namespace Gestus.DTOs.Grupo;

/// <summary>
/// Estatísticas detalhadas do grupo
/// </summary>
public class EstatisticasGrupo
{
    public int TotalUsuarios { get; set; }
    public int UsuariosAtivos { get; set; }
    public int UsuariosInativos { get; set; }
    public DateTime? UltimaAdesao { get; set; }
    public DateTime? PrimeiraAdesao { get; set; }
    public decimal TaxaCrescimentoMensal { get; set; }
    public Dictionary<string, int> DistribuicaoPorPapel { get; set; } = new();
}