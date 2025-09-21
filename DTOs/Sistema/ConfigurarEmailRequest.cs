using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Sistema;

/// <summary>
/// Request para configuração de email
/// </summary>
public class ConfigurarEmailRequest
{
    [Required(ErrorMessage = "Servidor SMTP é obrigatório")]
    [MaxLength(200, ErrorMessage = "Servidor SMTP deve ter no máximo 200 caracteres")]
    public string ServidorSmtp { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Porta é obrigatória")]
    [Range(1, 65535, ErrorMessage = "Porta deve estar entre 1 e 65535")]
    public int Porta { get; set; } = 587;
    
    [Required(ErrorMessage = "Email do remetente é obrigatório")]
    [EmailAddress(ErrorMessage = "Email do remetente deve ter formato válido")]
    [MaxLength(256, ErrorMessage = "Email deve ter no máximo 256 caracteres")]
    public string EmailRemetente { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Nome do remetente é obrigatório")]
    [MaxLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
    public string NomeRemetente { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Senha é obrigatória")]
    [MaxLength(500, ErrorMessage = "Senha deve ter no máximo 500 caracteres")]
    public string Senha { get; set; } = string.Empty;
    
    public bool UsarSsl { get; set; } = true;
    public bool UsarTls { get; set; } = true;
    public bool UsarAutenticacao { get; set; } = true;
    
    [EmailAddress(ErrorMessage = "Email de resposta deve ter formato válido")]
    [MaxLength(256, ErrorMessage = "Email de resposta deve ter no máximo 256 caracteres")]
    public string? EmailResposta { get; set; }
    
    [EmailAddress(ErrorMessage = "Email de cópia deve ter formato válido")]
    [MaxLength(256, ErrorMessage = "Email de cópia deve ter no máximo 256 caracteres")]
    public string? EmailCopia { get; set; }
}