namespace Gestus.DTOs.Papel;

/// <summary>
/// Estatísticas de uma operação
/// </summary>
public class EstatisticasOperacao
{
    /// <summary>
    /// Duração da operação
    /// </summary>
    public TimeSpan Duracao { get; set; }

    /// <summary>
    /// Recursos afetados
    /// </summary>
    public int RecursosAfetados { get; set; }

    /// <summary>
    /// Recursos processados com sucesso
    /// </summary>
    public int RecursosSucesso { get; set; }

    /// <summary>
    /// Recursos que falharam
    /// </summary>
    public int RecursosFalha { get; set; }

    /// <summary>
    /// Data e hora da operação
    /// </summary>
    public DateTime DataOperacao { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Dados adicionais da operação
    /// </summary>
    public Dictionary<string, object>? DadosAdicionais { get; set; }
}