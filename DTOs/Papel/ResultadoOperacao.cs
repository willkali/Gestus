namespace Gestus.DTOs.Papel;

/// <summary>
/// Resultado genérico de operação
/// </summary>
public class ResultadoOperacao
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Mensagem sobre o resultado
    /// </summary>
    public string Mensagem { get; set; } = string.Empty;

    /// <summary>
    /// Código de erro (se houver)
    /// </summary>
    public string? CodigoErro { get; set; }

    /// <summary>
    /// Detalhes adicionais sobre o resultado
    /// </summary>
    public Dictionary<string, object>? Detalhes { get; set; }

    /// <summary>
    /// Data da operação
    /// </summary>
    public DateTime DataOperacao { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cria um resultado de sucesso
    /// </summary>
    public static ResultadoOperacao CriarSucesso(string mensagem, Dictionary<string, object>? detalhes = null)
    {
        return new ResultadoOperacao
        {
            Sucesso = true,
            Mensagem = mensagem,
            Detalhes = detalhes
        };
    }

    /// <summary>
    /// Cria um resultado de erro
    /// </summary>
    public static ResultadoOperacao CriarErro(string mensagem, string? codigoErro = null, Dictionary<string, object>? detalhes = null)
    {
        return new ResultadoOperacao
        {
            Sucesso = false,
            Mensagem = mensagem,
            CodigoErro = codigoErro,
            Detalhes = detalhes
        };
    }
}