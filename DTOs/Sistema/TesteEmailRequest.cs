using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Sistema;

/// <summary>
/// Request para teste de email
/// </summary>
public class TesteEmailRequest
{
    [Required(ErrorMessage = "Email de destino é obrigatório")]
    [EmailAddress(ErrorMessage = "Email deve ter formato válido")]
    public string EmailDestino { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Tipo de template é obrigatório")]
    public string TipoTemplate { get; set; } = string.Empty;
    
    /// <summary>
    /// Dados para substituir no template
    /// </summary>
    public Dictionary<string, string>? VariaveisTemplate { get; set; }
}