namespace Gestus.DTOs.Aplicacao;

/// <summary>
/// Informações resumidas de status de aplicação
/// </summary>
public class StatusAplicacaoResumo
{
    /// <summary>
    /// ID do status
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Código do status
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Nome do status
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Cor de fundo
    /// </summary>
    public string CorFundo { get; set; } = string.Empty;

    /// <summary>
    /// Cor do texto
    /// </summary>
    public string CorTexto { get; set; } = string.Empty;

    /// <summary>
    /// Ícone do status
    /// </summary>
    public string? Icone { get; set; }
}