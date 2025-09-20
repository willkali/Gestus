using System.ComponentModel.DataAnnotations;
using Gestus.DTOs.Comuns;

namespace Gestus.DTOs.Papel;

/// <summary>
/// Filtros para usuários de um papel específico
/// </summary>
public class FiltrosUsuariosPapel : FiltrosBase
{
    /// <summary>
    /// Filtrar por nome do usuário
    /// </summary>
    [MaxLength(200)]
    public string? Nome { get; set; }

    /// <summary>
    /// Filtrar por email do usuário
    /// </summary>
    [MaxLength(256)]
    public string? Email { get; set; }

    /// <summary>
    /// Filtrar apenas usuários ativos
    /// </summary>
    public bool? Ativo { get; set; }

    /// <summary>
    /// Data início de atribuição do papel
    /// </summary>
    public DateTime? DataAtribuicaoInicio { get; set; }

    /// <summary>
    /// Data fim de atribuição do papel
    /// </summary>
    public DateTime? DataAtribuicaoFim { get; set; }

    /// <summary>
    /// Incluir usuários com papel expirado
    /// </summary>
    public bool IncluirExpirados { get; set; } = false;

    /// <summary>
    /// Ordenar por data de atribuição
    /// </summary>
    public bool OrdenarPorAtribuicao { get; set; } = false;
}