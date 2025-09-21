using System.ComponentModel.DataAnnotations;

namespace Gestus.Modelos;

/// <summary>
/// Versionamento de chaves de encriptação
/// </summary>
public class ChaveEncriptacao
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty; // Ex: EmailKey, FileKey
    
    [Required]
    public int Versao { get; set; } // 1, 2, 3, etc
    
    [Required]
    [MaxLength(500)]
    public string ChaveEncriptada { get; set; } = string.Empty; // Chave encriptada com master key
    
    public bool Ativa { get; set; } = true;
    
    /// <summary>
    /// Data de expiração da chave (opcional)
    /// </summary>
    public DateTime? DataExpiracao { get; set; }
    
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataDesativacao { get; set; }
    
    [MaxLength(500)]
    public string? Observacoes { get; set; }
}

/// <summary>
/// Log de uso de chaves para auditoria
/// </summary>
public class LogUsoChave
{
    public int Id { get; set; }
    
    public int ChaveEncriptacaoId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Operacao { get; set; } = string.Empty; // Encriptar, Descriptografar
    
    [Required]
    [MaxLength(100)]
    public string Contexto { get; set; } = string.Empty; // Email, Arquivo, Token
    
    [MaxLength(200)]
    public string? Identificador { get; set; } // ID do token, email, etc
    
    public bool Sucesso { get; set; }
    
    [MaxLength(500)]
    public string? MensagemErro { get; set; }
    
    public DateTime DataHora { get; set; } = DateTime.UtcNow;
    
    // Relacionamento
    public virtual ChaveEncriptacao ChaveEncriptacao { get; set; } = null!;
}