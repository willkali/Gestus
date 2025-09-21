using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Sistema;

/// <summary>
/// Request para criar template personalizado
/// </summary>
public class CriarTemplateRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string Nome { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Tipo é obrigatório")]
    [MaxLength(100, ErrorMessage = "Tipo deve ter no máximo 100 caracteres")]
    public string Tipo { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Assunto é obrigatório")]
    [MaxLength(300, ErrorMessage = "Assunto deve ter no máximo 300 caracteres")]
    public string Assunto { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Corpo HTML é obrigatório")]
    public string CorpoHtml { get; set; } = string.Empty;
    
    public string? CorpoTexto { get; set; }
    
    [MaxLength(500, ErrorMessage = "Descrição deve ter no máximo 500 caracteres")]
    public string? Descricao { get; set; }
}