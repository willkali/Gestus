using System.ComponentModel.DataAnnotations;

namespace Gestus.Modelos;

/// <summary>
/// Template personalizado criado pelo administrador
/// </summary>
public class TemplateEmailPersonalizado
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Tipo { get; set; } = string.Empty; // RecuperarSenha, BoasVindas, etc
    
    [Required]
    [MaxLength(300)]
    public string Assunto { get; set; } = string.Empty;
    
    [Required]
    public string CorpoHtml { get; set; } = string.Empty;
    
    public string? CorpoTexto { get; set; }
    
    [MaxLength(500)]
    public string? Descricao { get; set; }
    
    /// <summary>
    /// Lista de variáveis obrigatórias que devem estar presentes
    /// </summary>
    public string VariaveisObrigatorias { get; set; } = string.Empty; // JSON array
    
    /// <summary>
    /// Lista de variáveis opcionais disponíveis
    /// </summary>
    public string VariaveisOpcionais { get; set; } = string.Empty; // JSON array
    
    public bool Ativo { get; set; } = true;
    public bool IsTemplate { get; set; } = false; // true = template padrão do sistema
    
    public int CriadoPorId { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
    public int? AtualizadoPorId { get; set; }
    
    // Relacionamentos
    public virtual Usuario CriadoPor { get; set; } = null!;
    public virtual Usuario? AtualizadoPor { get; set; }
}

/// <summary>
/// Variáveis disponíveis por tipo de template
/// </summary>
public class VariavelTemplate
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Chave { get; set; } = string.Empty; // Ex: {NomeUsuario}
    
    [Required]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string TipoTemplate { get; set; } = string.Empty;
    
    public bool Obrigatoria { get; set; } = true;
    
    [MaxLength(200)]
    public string? ExemploValor { get; set; }
    
    public bool Ativo { get; set; } = true;
    
    public int Ordem { get; set; } = 0;
}