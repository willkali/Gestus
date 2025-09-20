using Gestus.DTOs.Comuns;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Estatísticas da busca realizada
/// </summary>
public class EstatisticasBusca
{
    public int TotalGruposAnalisados { get; set; }
    public int GruposAtivos { get; set; }
    public int GruposInativos { get; set; }
    public Dictionary<string, int> DistribuicaoPorTipo { get; set; } = new();
    public int MediaUsuariosPorGrupo { get; set; }
}