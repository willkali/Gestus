using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Sistema;

/// <summary>
/// Request para validar template
/// </summary>
public class ValidarTemplateRequest
{
    [Required(ErrorMessage = "Tipo é obrigatório")]
    public string Tipo { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Assunto é obrigatório")]
    public string Assunto { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Corpo HTML é obrigatório")]
    public string CorpoHtml { get; set; } = string.Empty;
    
    public string? CorpoTexto { get; set; }
}