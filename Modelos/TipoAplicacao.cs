using System.ComponentModel.DataAnnotations;

namespace Gestus.Modelos;

/// <summary>
/// Tipos de aplicação suportados pelo Gestus IAM
/// </summary>
public class TipoAplicacao
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty; // "webapi", "spa", "desktop", "mobile"
    
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty; // "Web API", "Single Page Application"
    
    [Required]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;
    
    /// <summary>
    /// Ícone para representar o tipo na interface (emoji, classe CSS ou nome do ícone)
    /// </summary>
    [MaxLength(50)]
    public string? Icone { get; set; } // "🌐", "fa-globe", "api-icon"
    
    /// <summary>
    /// Cor hexadecimal para representar o tipo na interface
    /// </summary>
    [MaxLength(7)]
    public string? Cor { get; set; } = "#6c757d"; // #007bff, #28a745, etc
    
    /// <summary>
    /// Campos específicos que este tipo suporta (JSON array)
    /// Exemplo: ["endpoint", "metodoHttp"] para WebAPI
    /// ["modulo", "tela"] para Desktop
    /// </summary>
    public string CamposPermissao { get; set; } = "[]";
    
    /// <summary>
    /// Schema JSON para validação das permissões deste tipo
    /// Define regras de validação específicas para cada tipo
    /// </summary>
    public string? SchemaValidacao { get; set; }
    
    /// <summary>
    /// Template padrão para criação de permissões (JSON)
    /// Facilita a criação rápida de permissões comuns
    /// </summary>
    public string? TemplatePermissao { get; set; } = "{}";
    
    /// <summary>
    /// Configurações específicas do tipo (JSON)
    /// Exemplo: {"requerAutenticacao": true, "suportaRefreshToken": false}
    /// </summary>
    public string? ConfiguracoesTipo { get; set; } = "{}";
    
    /// <summary>
    /// Instruções de integração/configuração para este tipo
    /// </summary>
    [MaxLength(1000)]
    public string? InstrucoesIntegracao { get; set; }
    
    public bool Ativo { get; set; } = true;
    public bool Padrao { get; set; } = false; // Tipos padrão do sistema (não podem ser deletados)
    public bool PermiteExclusao { get; set; } = true; // Controla se pode ser excluído
    public int Ordem { get; set; } = 0; // Para ordenação na interface
    
    /// <summary>
    /// Nível de complexidade (1-5): 1=Simples, 5=Complexo
    /// Usado para ordenar por complexidade na interface
    /// </summary>
    public int NivelComplexidade { get; set; } = 1;
    
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
    public int? CriadoPorId { get; set; }
    public int? AtualizadoPorId { get; set; }
    
    // Relacionamentos
    public virtual Usuario? CriadoPor { get; set; }
    public virtual Usuario? AtualizadoPor { get; set; }
    public virtual ICollection<Aplicacao> Aplicacoes { get; set; } = new List<Aplicacao>();
}