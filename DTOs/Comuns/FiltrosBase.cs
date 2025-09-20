using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Comuns;

/// <summary>
/// Classe base para filtros com paginação e ordenação
/// </summary>
public abstract class FiltrosBase
{
    /// <summary>
    /// Página atual (inicia em 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Página deve ser maior que zero")]
    public int Pagina { get; set; } = 1;

    /// <summary>
    /// Número de itens por página
    /// </summary>
    [Range(1, 100, ErrorMessage = "Itens por página deve estar entre 1 e 100")]
    public int ItensPorPagina { get; set; } = 20;

    /// <summary>
    /// Campo para ordenação
    /// </summary>
    public string? OrdenarPor { get; set; }

    /// <summary>
    /// Direção da ordenação (asc/desc)
    /// </summary>
    public string? DirecaoOrdenacao { get; set; } = "asc";

    /// <summary>
    /// Termo de busca geral (opcional)
    /// </summary>
    public string? TermoBusca { get; set; }

    /// <summary>
    /// Incluir registros inativos/removidos
    /// </summary>
    public bool IncluirInativos { get; set; } = false;
}