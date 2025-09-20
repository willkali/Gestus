using Gestus.DTOs.Comuns;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Filtros para busca de usuários de um grupo específico
/// </summary>
public class FiltrosUsuariosGrupo : FiltrosBase
{
    /// <summary>
    /// Filtro por nome ou sobrenome do usuário
    /// </summary>
    public string? Nome { get; set; }

    /// <summary>
    /// Filtro por email do usuário
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Filtro por status ativo/inativo do usuário
    /// </summary>
    public bool? Ativo { get; set; }

    /// <summary>
    /// Data de adesão início
    /// </summary>
    public DateTime? DataAdesaoInicio { get; set; }

    /// <summary>
    /// Data de adesão fim
    /// </summary>
    public DateTime? DataAdesaoFim { get; set; }

    /// <summary>
    /// Filtrar por papéis específicos
    /// </summary>
    public List<string>? Papeis { get; set; }

    /// <summary>
    /// Incluir apenas usuários ativos no grupo
    /// </summary>
    public bool ApenasAtivosNoGrupo { get; set; } = true;
}