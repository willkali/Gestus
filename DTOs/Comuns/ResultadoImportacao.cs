namespace Gestus.DTOs.Comuns;

/// <summary>
/// Resultado de operação de importação
/// </summary>
public class ResultadoImportacao
{
    /// <summary>
    /// Indica se a importação foi bem-sucedida
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Mensagem geral do resultado
    /// </summary>
    public string Mensagem { get; set; } = string.Empty;

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
    /// Resumo detalhado da operação
    /// </summary>
    public string Resumo { get; set; } = string.Empty;

    /// <summary>
    /// Duração da operação
    /// </summary>
    public TimeSpan Duracao { get; set; }

    /// <summary>
    /// Data e hora da operação
    /// </summary>
    public DateTime DataOperacao { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Dados adicionais da importação
    /// </summary>
    public object? DadosAdicionais { get; set; }
}

/// <summary>
/// Erro específico durante importação
/// </summary>
public class ErroImportacao
{
    /// <summary>
    /// Linha onde ocorreu o erro (se aplicável)
    /// </summary>
    public int? Linha { get; set; }

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
    /// Tipo/categoria do erro
    /// </summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>
    /// Código de erro (se aplicável)
    /// </summary>
    public string? Codigo { get; set; }

    /// <summary>
    /// Detalhes técnicos do erro
    /// </summary>
    public string? DetalhesEnicos { get; set; }
}