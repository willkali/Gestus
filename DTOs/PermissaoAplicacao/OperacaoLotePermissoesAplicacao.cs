using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.PermissaoAplicacao;

/// <summary>
/// Request para operações em lote com permissões de aplicação
/// </summary>
public class OperacaoLotePermissoesAplicacao
{
    /// <summary>
    /// Tipo de operação: ativar, desativar, excluir, categoria-alterar, nivel-alterar
    /// </summary>
    [Required(ErrorMessage = "Tipo de operação é obrigatório")]
    public string TipoOperacao { get; set; } = string.Empty;

    /// <summary>
    /// IDs das permissões para a operação
    /// </summary>
    [Required(ErrorMessage = "Lista de permissões é obrigatória")]
    [MinLength(1, ErrorMessage = "Deve haver pelo menos uma permissão")]
    public List<int> PermissoesIds { get; set; } = new();

    /// <summary>
    /// ID da aplicação (para validação)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "ID da aplicação deve ser válido")]
    public int? AplicacaoId { get; set; }

    /// <summary>
    /// Parâmetros específicos da operação
    /// </summary>
    public Dictionary<string, string> Parametros { get; set; } = new();

    /// <summary>
    /// Observações da operação
    /// </summary>
    [MaxLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres")]
    public string? Observacoes { get; set; }

    /// <summary>
    /// Forçar operação mesmo com avisos
    /// </summary>
    public bool ForcarOperacao { get; set; } = false;

    /// <summary>
    /// Notificar administradores sobre a operação
    /// </summary>
    public bool NotificarAdministradores { get; set; } = true;
}

/// <summary>
/// Resultado de operação em lote
/// </summary>
public class ResultadoOperacaoLotePermissoesAplicacao
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Mensagem geral do resultado
    /// </summary>
    public string Mensagem { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de operação executada
    /// </summary>
    public string TipoOperacao { get; set; } = string.Empty;

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
    public List<ResultadoIndividualPermissaoAplicacao> Detalhes { get; set; } = new();

    /// <summary>
    /// Resumo da operação
    /// </summary>
    public string Resumo { get; set; } = string.Empty;

    /// <summary>
    /// Duração da operação
    /// </summary>
    public TimeSpan Duracao { get; set; }

    /// <summary>
    /// Avisos gerados durante a operação
    /// </summary>
    public List<string> Avisos { get; set; } = new();

    /// <summary>
    /// Erros gerais da operação
    /// </summary>
    public List<string> Erros { get; set; } = new();
}

/// <summary>
/// Resultado individual de uma permissão na operação em lote
/// </summary>
public class ResultadoIndividualPermissaoAplicacao
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

    /// <summary>
    /// Estado anterior (para rollback)
    /// </summary>
    public object? EstadoAnterior { get; set; }

    /// <summary>
    /// Estado atual
    /// </summary>
    public object? EstadoAtual { get; set; }
}