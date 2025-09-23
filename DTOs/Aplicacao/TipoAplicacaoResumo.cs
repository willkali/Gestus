namespace Gestus.DTOs.Aplicacao;

/// <summary>
/// Informações resumidas de tipo de aplicação
/// </summary>
public class TipoAplicacaoResumo
{
    /// <summary>
    /// ID do tipo
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Código do tipo
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Nome do tipo
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Ícone do tipo
    /// </summary>
    public string? Icone { get; set; }

    /// <summary>
    /// Cor do tipo
    /// </summary>
    public string? Cor { get; set; }
}