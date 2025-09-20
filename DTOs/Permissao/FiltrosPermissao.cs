using Gestus.DTOs.Comuns;

namespace Gestus.DTOs.Permissao;

/// <summary>
/// Filtros para busca de permissões
/// </summary>
public class FiltrosPermissao : FiltrosBase
{
    /// <summary>
    /// Filtrar por nome da permissão
    /// </summary>
    public string? Nome { get; set; }

    /// <summary>
    /// Filtrar por descrição da permissão
    /// </summary>
    public string? Descricao { get; set; }

    /// <summary>
    /// Filtrar por recurso da permissão
    /// </summary>
    public string? Recurso { get; set; }

    /// <summary>
    /// Filtrar por ação da permissão
    /// </summary>
    public string? Acao { get; set; }

    /// <summary>
    /// Filtrar por categoria da permissão
    /// </summary>
    public string? Categoria { get; set; }

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
    /// Incluir permissões inativas nos resultados
    /// </summary>
    public bool IncluirInativas { get; set; } = false;
}