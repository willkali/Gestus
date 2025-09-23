using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Aplicacao;

/// <summary>
/// Filtros para busca de aplicações
/// </summary>
public class FiltrosAplicacao
{
    /// <summary>
    /// Número da página (padrão: 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Página deve ser maior que 0")]
    public int Pagina { get; set; } = 1;

    /// <summary>
    /// Quantidade de itens por página (padrão: 20, máximo: 100)
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
    /// Filtro por nome da aplicação
    /// </summary>
    [MaxLength(100)]
    public string? Nome { get; set; }

    /// <summary>
    /// Filtro por código da aplicação
    /// </summary>
    [MaxLength(50)]
    public string? Codigo { get; set; }

    /// <summary>
    /// Filtro por tipo de aplicação
    /// </summary>
    public int? TipoAplicacaoId { get; set; }

    /// <summary>
    /// Filtro por status da aplicação
    /// </summary>
    public int? StatusAplicacaoId { get; set; }

    /// <summary>
    /// Filtro por aplicações ativas/inativas
    /// </summary>
    public bool? Ativa { get; set; }

    /// <summary>
    /// Filtro por nível mínimo de segurança
    /// </summary>
    [Range(1, 10)]
    public int? NivelSegurancaMinimo { get; set; }

    /// <summary>
    /// Filtro por nível máximo de segurança
    /// </summary>
    [Range(1, 10)]
    public int? NivelSegurancaMaximo { get; set; }

    /// <summary>
    /// Filtro por data de criação inicial
    /// </summary>
    public DateTime? DataCriacaoInicio { get; set; }

    /// <summary>
    /// Filtro por data de criação final
    /// </summary>
    public DateTime? DataCriacaoFim { get; set; }

    /// <summary>
    /// Incluir aplicações inativas na busca
    /// </summary>
    public bool IncluirInativas { get; set; } = false;

    /// <summary>
    /// Busca textual livre
    /// </summary>
    [MaxLength(200)]
    public string? TermoBusca { get; set; }
}