using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Sistema;

/// <summary>
/// Template de email
/// </summary>
public class TemplateEmailResponse
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Assunto { get; set; } = string.Empty;
    public string CorpoHtml { get; set; } = string.Empty;
    public string? CorpoTexto { get; set; }
    public bool Ativo { get; set; }
}