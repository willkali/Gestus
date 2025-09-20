using Gestus.DTOs.Comuns;

namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Filtros para busca de associações papel-permissão
/// </summary>
public class FiltrosAssociacoes : FiltrosBase
{
    /// <summary>
    /// IDs específicos de papéis
    /// </summary>
    public List<int>? PapeisIds { get; set; }

    /// <summary>
    /// IDs específicos de permissões
    /// </summary>
    public List<int>? PermissoesIds { get; set; }

    /// <summary>
    /// Filtrar por categoria de papel
    /// </summary>
    public string? CategoriaPapel { get; set; }

    /// <summary>
    /// Filtrar por categoria de permissão
    /// </summary>
    public string? CategoriaPermissao { get; set; }

    /// <summary>
    /// Filtrar por status ativo/inativo
    /// </summary>
    public bool? Ativo { get; set; }

    /// <summary>
    /// Data de atribuição início
    /// </summary>
    public DateTime? DataAtribuicaoInicio { get; set; }

    /// <summary>
    /// Data de atribuição fim
    /// </summary>
    public DateTime? DataAtribuicaoFim { get; set; }

    /// <summary>
    /// Filtrar apenas associações órfãs (sem papel ou permissão ativa)
    /// </summary>
    public bool ApenasAssociacoesOrfas { get; set; } = false;
}