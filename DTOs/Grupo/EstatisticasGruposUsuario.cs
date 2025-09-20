namespace Gestus.DTOs.Grupo;

/// <summary>
/// Estatísticas dos grupos do usuário
/// </summary>
public class EstatisticasGruposUsuario
{
    public int TotalGrupos { get; set; }
    public int GruposAtivos { get; set; }
    public int GruposInativos { get; set; }
    public DateTime? PrimeiraAdesao { get; set; }
    public DateTime? UltimaAdesao { get; set; }
    public Dictionary<string, int> DistribuicaoPorTipo { get; set; } = new();
    public int MediaMembrosNosGrupos { get; set; }
    public string GrupoMaisAtivo { get; set; } = string.Empty;
}