namespace Gestus.DTOs.Permissao;

/// <summary>
/// Resultado de operação em lote com permissões
/// </summary>
public class ResultadoOperacaoLote
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Total de permissões processadas
    /// </summary>
    public int TotalProcessadas { get; set; }

    /// <summary>
    /// Total de sucessos
    /// </summary>
    public int TotalSucessos { get; set; }

    /// <summary>
    /// Total de falhas
    /// </summary>
    public int TotalFalhas { get; set; }

    /// <summary>
    /// Detalhes dos resultados por permissão
    /// </summary>
    public List<ResultadoIndividual> Detalhes { get; set; } = new();

    /// <summary>
    /// Resumo da operação
    /// </summary>
    public string Resumo { get; set; } = string.Empty;

    /// <summary>
    /// Duração da operação
    /// </summary>
    public TimeSpan Duracao { get; set; }
}

/// <summary>
/// Resultado individual de uma permissão na operação em lote
/// </summary>
public class ResultadoIndividual
{
    /// <summary>
    /// ID da permissão processada
    /// </summary>
    public int PermissaoId { get; set; }

    /// <summary>
    /// Nome da permissão
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Indica se a operação foi bem-sucedida para esta permissão
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Mensagem de erro (se houver)
    /// </summary>
    public string? Erro { get; set; }

    /// <summary>
    /// Detalhes adicionais
    /// </summary>
    public string? Detalhes { get; set; }
}