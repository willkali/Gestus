namespace Gestus.DTOs.Permissao;

/// <summary>
/// Estatísticas gerais do sistema de permissões
/// </summary>
public class EstatisticasGeraisPermissoes
{
    /// <summary>
    /// Total de permissões no sistema
    /// </summary>
    public int TotalPermissoes { get; set; }

    /// <summary>
    /// Total de permissões ativas
    /// </summary>
    public int PermissoesAtivas { get; set; }

    /// <summary>
    /// Total de permissões inativas
    /// </summary>
    public int PermissoesInativas { get; set; }

    /// <summary>
    /// Total de categorias
    /// </summary>
    public int TotalCategorias { get; set; }

    /// <summary>
    /// Permissões mais utilizadas
    /// </summary>
    public List<PermissaoUsada> PermissoesMaisUsadas { get; set; } = new();

    /// <summary>
    /// Permissões menos utilizadas
    /// </summary>
    public List<PermissaoUsada> PermissoesMenosUsadas { get; set; } = new();

    /// <summary>
    /// Categorias com mais permissões
    /// </summary>
    public List<CategoriaComContagem> CategoriasMaisUsadas { get; set; } = new();

    /// <summary>
    /// Recursos com mais ações
    /// </summary>
    public List<RecursoComContagem> RecursosMaisUsados { get; set; } = new();

    /// <summary>
    /// Data de geração das estatísticas
    /// </summary>
    public DateTime DataGeracao { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Permissão com estatísticas de uso
/// </summary>
public class PermissaoUsada
{
    /// <summary>
    /// ID da permissão
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nome da permissão
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Descrição da permissão
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Categoria da permissão
    /// </summary>
    public string? Categoria { get; set; }

    /// <summary>
    /// Número de papéis que têm a permissão
    /// </summary>
    public int TotalPapeis { get; set; }

    /// <summary>
    /// Número de usuários que têm a permissão
    /// </summary>
    public int TotalUsuarios { get; set; }

    /// <summary>
    /// Percentual de uso em relação ao total
    /// </summary>
    public decimal PercentualUso { get; set; }
}

/// <summary>
/// Categoria com contagem
/// </summary>
public class CategoriaComContagem
{
    /// <summary>
    /// Nome da categoria
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Total de permissões na categoria
    /// </summary>
    public int TotalPermissoes { get; set; }

    /// <summary>
    /// Percentual em relação ao total
    /// </summary>
    public decimal Percentual { get; set; }
}

/// <summary>
/// Recurso com contagem de ações
/// </summary>
public class RecursoComContagem
{
    /// <summary>
    /// Nome do recurso
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Total de ações no recurso
    /// </summary>
    public int TotalAcoes { get; set; }

    /// <summary>
    /// Lista de ações disponíveis
    /// </summary>
    public List<string> Acoes { get; set; } = new();
}