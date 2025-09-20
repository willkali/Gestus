using Gestus.DTOs.Comuns;

namespace Gestus.DTOs.Papel;

/// <summary>
/// Filtros para busca de papéis
/// </summary>
public class FiltrosPapel : FiltrosBase
{
    /// <summary>
    /// Filtrar por nome do papel
    /// </summary>
    public string? Nome { get; set; }

    /// <summary>
    /// Filtrar por descrição do papel
    /// </summary>
    public string? Descricao { get; set; }

    /// <summary>
    /// Filtrar por categoria do papel
    /// </summary>
    public string? Categoria { get; set; }

    /// <summary>
    /// Filtrar por nível mínimo
    /// </summary>
    public int? NivelMinimo { get; set; }

    /// <summary>
    /// Filtrar por nível máximo
    /// </summary>
    public int? NivelMaximo { get; set; }

    /// <summary>
    /// Filtrar por status ativo/inativo
    /// </summary>
    public bool? Ativo { get; set; }

    /// <summary>
    /// Data de criação início para filtro de período
    /// </summary>
    public DateTime? DataCriacaoInicio { get; set; }

    /// <summary>
    /// Data de criação fim para filtro de período
    /// </summary>
    public DateTime? DataCriacaoFim { get; set; }

    /// <summary>
    /// Filtrar apenas papéis do sistema
    /// </summary>
    public bool ApenasRolesSistema { get; set; } = false;

    /// <summary>
    /// Filtrar apenas papéis personalizadas
    /// </summary>
    public bool ApenasRolesPersonalizadas { get; set; } = false;

    /// <summary>
    /// Lista de permissões que o papel deve ter
    /// </summary>
    public List<string>? Permissoes { get; set; }

    // ✅ REMOVIDO: IncluirInativos (já existe na classe base FiltrosBase)
    // A propriedade IncluirInativos da classe base será usada automaticamente
}