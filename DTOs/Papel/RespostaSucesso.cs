namespace Gestus.DTOs.Comuns;

/// <summary>
/// Resposta genérica de sucesso
/// </summary>
public class RespostaSucesso
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Mensagem de sucesso
    /// </summary>
    public string Mensagem { get; set; } = string.Empty;
    
    /// <summary>
    /// Dados adicionais da operação
    /// </summary>
    public object? Dados { get; set; }
    
    /// <summary>
    /// Data e hora da operação
    /// </summary>
    public DateTime DataOperacao { get; set; } = DateTime.UtcNow;
}