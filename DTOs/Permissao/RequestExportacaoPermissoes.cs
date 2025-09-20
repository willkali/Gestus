namespace Gestus.DTOs.Permissao;

/// <summary>
/// Request para exportação de permissões
/// </summary>
public class RequestExportacaoPermissoes
{
    /// <summary>
    /// Formato de exportação: csv, xlsx, json, xml
    /// </summary>
    public string Formato { get; set; } = "csv";

    /// <summary>
    /// IDs específicas para exportar (se vazio, exporta todos)
    /// </summary>
    public List<int>? PermissoesIds { get; set; }

    /// <summary>
    /// Filtros para exportação
    /// </summary>
    public FiltrosPermissao? Filtros { get; set; }

    /// <summary>
    /// Incluir permissões inativas
    /// </summary>
    public bool IncluirInativas { get; set; } = false;

    /// <summary>
    /// Incluir relacionamentos (papéis, usuários)
    /// </summary>
    public bool IncluirRelacionamentos { get; set; } = false;

    /// <summary>
    /// Incluir estatísticas de uso
    /// </summary>
    public bool IncluirEstatisticas { get; set; } = false;
}

/// <summary>
/// Request para importação de permissões
/// </summary>
public class RequestImportacaoPermissoes
{
    /// <summary>
    /// Formato dos dados: csv, xlsx, json, xml
    /// </summary>
    public string Formato { get; set; } = "csv";

    /// <summary>
    /// Dados para importação (base64 ou JSON)
    /// </summary>
    public string Dados { get; set; } = string.Empty;

    /// <summary>
    /// Modo de importação: criar, atualizar, substituir
    /// </summary>
    public string Modo { get; set; } = "criar";

    /// <summary>
    /// Ignorar erros de validação (não recomendado)
    /// </summary>
    public bool IgnorarErros { get; set; } = false;

    /// <summary>
    /// Validar apenas (não executar importação)
    /// </summary>
    public bool ApenasValidar { get; set; } = false;
}

/// <summary>
/// Resultado de importação
/// </summary>
public class ResultadoImportacao
{
    /// <summary>
    /// Indica se a importação foi bem-sucedida
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Total de registros processados
    /// </summary>
    public int TotalProcessados { get; set; }

    /// <summary>
    /// Total de sucessos
    /// </summary>
    public int TotalSucessos { get; set; }

    /// <summary>
    /// Total de falhas
    /// </summary>
    public int TotalFalhas { get; set; }

    /// <summary>
    /// Total de registros ignorados
    /// </summary>
    public int TotalIgnorados { get; set; }

    /// <summary>
    /// Lista de erros encontrados
    /// </summary>
    public List<ErroImportacao> Erros { get; set; } = new();

    /// <summary>
    /// Lista de avisos
    /// </summary>
    public List<string> Avisos { get; set; } = new();

    /// <summary>
    /// Resumo da operação
    /// </summary>
    public string Resumo { get; set; } = string.Empty;
}

/// <summary>
/// Erro de importação
/// </summary>
public class ErroImportacao
{
    /// <summary>
    /// Linha onde ocorreu o erro
    /// </summary>
    public int Linha { get; set; }

    /// <summary>
    /// Campo onde ocorreu o erro
    /// </summary>
    public string? Campo { get; set; }

    /// <summary>
    /// Valor que causou o erro
    /// </summary>
    public string? Valor { get; set; }

    /// <summary>
    /// Mensagem de erro
    /// </summary>
    public string Mensagem { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de erro
    /// </summary>
    public string Tipo { get; set; } = string.Empty;
}