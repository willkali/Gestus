namespace Gestus.DTOs.PermissaoAplicacao;

/// <summary>
/// Estatísticas gerais de permissões de uma aplicação específica
/// </summary>
public class EstatisticasAplicacaoPermissoes
{
    /// <summary>
    /// ID da aplicação
    /// </summary>
    public int AplicacaoId { get; set; }

    /// <summary>
    /// Nome da aplicação
    /// </summary>
    public string NomeAplicacao { get; set; } = string.Empty;

    /// <summary>
    /// Código da aplicação
    /// </summary>
    public string CodigoAplicacao { get; set; } = string.Empty;

    /// <summary>
    /// Total de permissões da aplicação
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
    /// Total de recursos únicos
    /// </summary>
    public int TotalRecursos { get; set; }

    /// <summary>
    /// Total de ações únicas
    /// </summary>
    public int TotalAcoes { get; set; }

    /// <summary>
    /// Permissões mais utilizadas
    /// </summary>
    public List<PermissaoMaisUsada> PermissoesMaisUsadas { get; set; } = new();

    /// <summary>
    /// Recursos com mais permissões
    /// </summary>
    public List<RecursoEstatistica> RecursosMaisUsados { get; set; } = new();

    /// <summary>
    /// Categorias com mais permissões
    /// </summary>
    public List<CategoriaEstatistica> CategoriasMaisUsadas { get; set; } = new();

    /// <summary>
    /// Distribuição por nível de privilégio
    /// </summary>
    public List<NivelEstatistica> DistribuicaoPorNivel { get; set; } = new();

    /// <summary>
    /// Distribuição por campos específicos do tipo de aplicação
    /// </summary>
    public Dictionary<string, List<CampoEstatistica>> CamposEspecificos { get; set; } = new();

    /// <summary>
    /// Data de geração das estatísticas
    /// </summary>
    public DateTime DataGeracao { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Permissão mais usada com estatísticas
/// </summary>
public class PermissaoMaisUsada
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Recurso { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public int TotalPapeis { get; set; }
    public int TotalUsuarios { get; set; }
    public decimal PercentualUso { get; set; }
}

/// <summary>
/// Estatística de recurso
/// </summary>
public class RecursoEstatistica
{
    public string Nome { get; set; } = string.Empty;
    public int TotalPermissoes { get; set; }
    public List<string> Acoes { get; set; } = new();
    public decimal PercentualTotal { get; set; }
}

/// <summary>
/// Estatística de categoria
/// </summary>
public class CategoriaEstatistica
{
    public string Nome { get; set; } = string.Empty;
    public int TotalPermissoes { get; set; }
    public decimal PercentualTotal { get; set; }
}

/// <summary>
/// Estatística de nível
/// </summary>
public class NivelEstatistica
{
    public int Nivel { get; set; }
    public string DescricaoNivel { get; set; } = string.Empty;
    public int TotalPermissoes { get; set; }
    public decimal PercentualTotal { get; set; }
}

/// <summary>
/// Estatística de campo específico
/// </summary>
public class CampoEstatistica
{
    public string Valor { get; set; } = string.Empty;
    public int TotalPermissoes { get; set; }
    public decimal PercentualTotal { get; set; }
}