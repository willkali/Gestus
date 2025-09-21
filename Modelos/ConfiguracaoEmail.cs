using System.ComponentModel.DataAnnotations;

namespace Gestus.Modelos;

/// <summary>
/// Configurações de email do sistema
/// </summary>
public class ConfiguracaoEmail
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string ServidorSmtp { get; set; } = string.Empty;
    
    [Range(1, 65535)]
    public int Porta { get; set; } = 587;
    
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string EmailRemetente { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string NomeRemetente { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string SenhaEncriptada { get; set; } = string.Empty;
    
    public bool UsarSsl { get; set; } = true;
    public bool UsarTls { get; set; } = true;
    public bool UsarAutenticacao { get; set; } = true;
    
    [MaxLength(256)]
    public string? EmailResposta { get; set; }
    
    [MaxLength(256)]
    public string? EmailCopia { get; set; }
    
    [MaxLength(256)]
    public string? EmailCopiaOculta { get; set; }
    
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
    
    /// <summary>
    /// Configurações específicas por tipo de email
    /// </summary>
    public virtual ICollection<TemplateEmail> Templates { get; set; } = new List<TemplateEmail>();
}

/// <summary>
/// Templates de email por categoria
/// </summary>
public class TemplateEmail
{
    public int Id { get; set; }
    
    public int ConfiguracaoEmailId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Tipo { get; set; } = string.Empty; // RecuperarSenha, BoasVindas, ConfirmarEmail, etc
    
    [Required]
    [MaxLength(300)]
    public string Assunto { get; set; } = string.Empty;
    
    [Required]
    public string CorpoHtml { get; set; } = string.Empty;
    
    public string? CorpoTexto { get; set; }
    
    public bool Ativo { get; set; } = true;
    
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
    
    // Relacionamento
    public virtual ConfiguracaoEmail ConfiguracaoEmail { get; set; } = null!;
}