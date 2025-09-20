using System.ComponentModel.DataAnnotations;
using Gestus.DTOs.Comuns;

namespace Gestus.DTOs.Papel;

/// <summary>
/// Filtros para listagem e busca de papéis
/// </summary>
public class FiltrosPapel : FiltrosBase
{
    /// <summary>
    /// Filtrar por nome do papel
    /// </summary>
    [MaxLength(100)]
    public string? Nome { get; set; }

    /// <summary>
    /// Filtrar por descrição do papel
    /// </summary>
    [MaxLength(200)]
    public string? Descricao { get; set; }

    /// <summary>
    /// Filtrar por categoria do papel
    /// </summary>
    [MaxLength(100)]
    public string? Categoria { get; set; }

    /// <summary>
    /// Filtrar papéis ativos ou inativos
    /// </summary>
    public bool? Ativo { get; set; }

    /// <summary>
    /// Filtrar por nível mínimo do papel
    /// </summary>
    [Range(1, 999)]
    public int? NivelMinimo { get; set; }

    /// <summary>
    /// Filtrar por nível máximo do papel
    /// </summary>
    [Range(1, 999)]
    public int? NivelMaximo { get; set; }

    /// <summary>
    /// Filtrar por data de criação início
    /// </summary>
    public DateTime? DataCriacaoInicio { get; set; }

    /// <summary>
    /// Filtrar por data de criação fim
    /// </summary>
    public DateTime? DataCriacaoFim { get; set; }

    /// <summary>
    /// Filtrar papéis que possuem essas permissões específicas
    /// </summary>
    public List<string>? Permissoes { get; set; }

    /// <summary>
    /// Incluir papéis inativos na busca
    /// </summary>
    public bool IncluirInativos { get; set; } = false;

    /// <summary>
    /// Buscar apenas papéis do sistema (SuperAdmin, Admin, etc.)
    /// </summary>
    public bool ApenasRolesSistema { get; set; } = false;

    /// <summary>
    /// Buscar apenas papéis personalizados (criados pelo usuário)
    /// </summary>
    public bool ApenasRolesPersonalizadas { get; set; } = false;
}