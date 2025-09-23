using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Filtros para busca de aplicações do usuário
/// </summary>
public class FiltrosAplicacaoUsuario
{
    /// <summary>
    /// Filtro por nome da aplicação
    /// </summary>
    [MaxLength(100)]
    public string? Nome { get; set; }

    /// <summary>
    /// Filtro por tipo de aplicação
    /// </summary>
    public string? TipoAplicacao { get; set; }

    /// <summary>
    /// Filtro por status da aplicação
    /// </summary>
    public string? StatusAplicacao { get; set; }

    /// <summary>
    /// Filtro por status do acesso do usuário
    /// </summary>
    public string? StatusAcesso { get; set; }

    /// <summary>
    /// Filtrar apenas aplicações ativas
    /// </summary>
    public bool? ApenasAtivas { get; set; } = true;

    /// <summary>
    /// Filtrar apenas acessos aprovados
    /// </summary>
    public bool? ApenasAprovados { get; set; }

    /// <summary>
    /// Filtrar por nível mínimo de segurança
    /// </summary>
    [Range(1, 10)]
    public int? NivelSegurancaMinimo { get; set; }

    /// <summary>
    /// Filtrar por nível máximo de segurança
    /// </summary>
    [Range(1, 10)]
    public int? NivelSegurancaMaximo { get; set; }

    /// <summary>
    /// Data início para filtrar por data de solicitação
    /// </summary>
    public DateTime? DataSolicitacaoInicio { get; set; }

    /// <summary>
    /// Data fim para filtrar por data de solicitação
    /// </summary>
    public DateTime? DataSolicitacaoFim { get; set; }

    /// <summary>
    /// Incluir aplicações expiradas
    /// </summary>
    public bool IncluirExpiradas { get; set; } = false;

    /// <summary>
    /// Incluir aplicações suspensas
    /// </summary>
    public bool IncluirSuspensas { get; set; } = false;

    /// <summary>
    /// Campo para ordenação
    /// </summary>
    public string? OrdenarPor { get; set; } = "Nome";

    /// <summary>
    /// Direção da ordenação (asc/desc)
    /// </summary>
    public string? DirecaoOrdenacao { get; set; } = "asc";

    /// <summary>
    /// Página atual (baseada em 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Página deve ser maior que 0")]
    public int Pagina { get; set; } = 1;

    /// <summary>
    /// Itens por página
    /// </summary>
    [Range(1, 100, ErrorMessage = "Itens por página deve ser entre 1 e 100")]
    public int ItensPorPagina { get; set; } = 20;
}