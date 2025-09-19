using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Resposta da busca avançada
/// </summary>
public class RespostaBuscaAvancada
{
    public List<UsuarioBuscaResultado> Resultados { get; set; } = new();
    public int TotalEncontrados { get; set; }
    public int TotalPaginas { get; set; }
    public int PaginaAtual { get; set; }
    public int ItensPorPagina { get; set; }
    public bool TemProximaPagina { get; set; }
    public bool TemPaginaAnterior { get; set; }
    public SolicitacaoBuscaAvancada CriteriosBusca { get; set; } = new();
    public EstatisticasAgregadas EstatisticasAgregadas { get; set; } = new();
    public TimeSpan TempoExecucao { get; set; }
    public DateTime ExecutadoEm { get; set; }
}