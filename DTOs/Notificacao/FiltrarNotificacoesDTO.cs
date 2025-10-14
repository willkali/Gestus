namespace Gestus.DTOs.Notificacao;

/// <summary>
/// DTO para filtrar notificações
/// </summary>
public class FiltrarNotificacoesDTO
{
    public int Pagina { get; set; } = 1;
    public int ItensPorPagina { get; set; } = 10;
    public bool? ApenasNaoLidas { get; set; }
    public List<string>? Tipos { get; set; }
    public List<int>? Prioridades { get; set; }
    public List<string>? Origens { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public string? TextoPesquisa { get; set; }
    public string? OrdenarPor { get; set; } = "DataCriacao";
    public bool OrdemDecrescente { get; set; } = true;
}
