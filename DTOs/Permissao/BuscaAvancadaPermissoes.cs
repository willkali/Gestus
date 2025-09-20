using Gestus.DTOs.Comuns;

namespace Gestus.DTOs.Permissao;

/// <summary>
/// Request para busca avançada de permissões
/// </summary>
public class BuscaAvancadaPermissoes : FiltrosBase
{
    /// <summary>
    /// Texto livre para busca em qualquer campo
    /// </summary>
    public string? TextoLivre { get; set; }

    /// <summary>
    /// Buscar por nomes exatos de permissões
    /// </summary>
    public List<string>? NomesExatos { get; set; }

    /// <summary>
    /// Buscar por recursos específicos
    /// </summary>
    public List<string>? Recursos { get; set; }

    /// <summary>
    /// Buscar por ações específicas
    /// </summary>
    public List<string>? Acoes { get; set; }

    /// <summary>
    /// Buscar por categorias específicas
    /// </summary>
    public List<string>? Categorias { get; set; }

    /// <summary>
    /// Buscar permissões que estão em uso (atribuídas a papéis)
    /// </summary>
    public bool? EmUso { get; set; }

    /// <summary>
    /// Buscar permissões criadas por período
    /// </summary>
    public DateTime? CriadaApartirDe { get; set; }

    /// <summary>
    /// Buscar permissões criadas até
    /// </summary>
    public DateTime? CriadaAte { get; set; }

    /// <summary>
    /// Número mínimo de papéis que devem ter a permissão
    /// </summary>
    public int? MinimoPapeis { get; set; }

    /// <summary>
    /// Número máximo de papéis que devem ter a permissão
    /// </summary>
    public int? MaximoPapeis { get; set; }

    /// <summary>
    /// Critérios de ordenação múltiplos
    /// </summary>
    public List<CriterioOrdenacao>? Ordenacao { get; set; }

    /// <summary>
    /// Incluir estatísticas de uso na resposta
    /// </summary>
    public bool IncluirEstatisticas { get; set; } = false;
}

/// <summary>
/// Critério de ordenação
/// </summary>
public class CriterioOrdenacao
{
    /// <summary>
    /// Campo para ordenação
    /// </summary>
    public string Campo { get; set; } = string.Empty;

    /// <summary>
    /// Direção da ordenação (asc, desc)
    /// </summary>
    public string Direcao { get; set; } = "asc";

    /// <summary>
    /// Prioridade da ordenação (1 = maior prioridade)
    /// </summary>
    public int Prioridade { get; set; } = 1;
}