using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Comuns;

/// <summary>
/// Resposta paginada genérica
/// </summary>
public class RespostaPaginada<T>
{
    public List<T> Dados { get; set; } = new();
    public int PaginaAtual { get; set; }
    public int ItensPorPagina { get; set; }
    public int TotalItens { get; set; }
    public int TotalPaginas { get; set; }
    public bool TemProximaPagina { get; set; }
    public bool TemPaginaAnterior { get; set; }
}

/// <summary>
/// Resposta genérica de erro
/// </summary>
public class RespostaErro
{
    public string Erro { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public List<string>? Detalhes { get; set; }
}