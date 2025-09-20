using Gestus.DTOs.Comuns;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Filtros para busca de grupos
/// </summary>
public class FiltrosGrupo : FiltrosBase
{
    /// <summary>
    /// Filtro por nome do grupo
    /// </summary>
    public string? Nome { get; set; }

    /// <summary>
    /// Filtro por descrição
    /// </summary>
    public string? Descricao { get; set; }

    /// <summary>
    /// Filtro por tipo de grupo
    /// </summary>
    public string? Tipo { get; set; }

    /// <summary>
    /// Filtro por status ativo/inativo
    /// </summary>
    public bool? Ativo { get; set; }

    /// <summary>
    /// Data de criação início
    /// </summary>
    public DateTime? DataCriacaoInicio { get; set; }

    /// <summary>
    /// Data de criação fim
    /// </summary>
    public DateTime? DataCriacaoFim { get; set; }

    /// <summary>
    /// Filtrar apenas grupos com usuários
    /// </summary>
    public bool? ApenasComUsuarios { get; set; }

    /// <summary>
    /// Número mínimo de usuários no grupo
    /// </summary>
    public int? MinUsuarios { get; set; }

    /// <summary>
    /// Número máximo de usuários no grupo
    /// </summary>
    public int? MaxUsuarios { get; set; }

    /// <summary>
    /// IDs específicos de grupos para filtrar
    /// </summary>
    public List<int>? GruposIds { get; set; }
}