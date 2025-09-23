using System.ComponentModel.DataAnnotations;

namespace Gestus.Modelos;

/// <summary>
/// Status/Estados possíveis para uma aplicação
/// </summary>
public class StatusAplicacao
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty; // "ativa", "manutencao", "desabilitada", "desenvolvimento"
    
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty; // "Ativa", "Em Manutenção", "Desabilitada"
    
    [Required]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;
    
    /// <summary>
    /// Cor de fundo para representar o status (hexadecimal)
    /// </summary>
    [MaxLength(7)]
    public string CorFundo { get; set; } = "#6c757d"; // #28a745 (verde), #ffc107 (amarelo), #dc3545 (vermelho)
    
    /// <summary>
    /// Cor do texto para garantir contraste (hexadecimal)
    /// </summary>
    [MaxLength(7)]
    public string CorTexto { get; set; } = "#ffffff";
    
    /// <summary>
    /// Ícone para representar o status
    /// </summary>
    [MaxLength(50)]
    public string? Icone { get; set; } // "✅", "⚠️", "❌", "🔧", "🚧"
    
    /// <summary>
    /// Define se aplicações com este status podem ser acessadas por usuários
    /// </summary>
    public bool PermiteAcesso { get; set; } = true;
    
    /// <summary>
    /// Define se aplicações com este status podem receber novos usuários
    /// </summary>
    public bool PermiteNovoUsuario { get; set; } = true;
    
    /// <summary>
    /// Define se aplicações com este status aparecem na listagem para usuários normais
    /// </summary>
    public bool VisivelParaUsuarios { get; set; } = true;
    
    /// <summary>
    /// Ações automáticas que devem ser executadas quando uma aplicação muda para este status (JSON)
    /// Exemplo: {"notificarAdmins": true, "enviarEmail": true, "logAuditoria": true}
    /// </summary>
    public string? AcoesAutomaticas { get; set; } = "{}";
    
    /// <summary>
    /// Mensagem exibida aos usuários quando tentam acessar aplicação neste status
    /// </summary>
    [MaxLength(500)]
    public string? MensagemUsuario { get; set; }
    
    public bool Ativo { get; set; } = true;
    public bool Padrao { get; set; } = false; // Status padrão do sistema
    public bool PermiteExclusao { get; set; } = true;
    public int Ordem { get; set; } = 0; // Para ordenação na interface
    
    /// <summary>
    /// Prioridade do status (1-10): 1=Baixa, 10=Crítica
    /// Usado para alertas e notificações
    /// </summary>
    public int Prioridade { get; set; } = 5;
    
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
    public int? CriadoPorId { get; set; }
    public int? AtualizadoPorId { get; set; }
    
    // Relacionamentos
    public virtual Usuario? CriadoPor { get; set; }
    public virtual Usuario? AtualizadoPor { get; set; }
    public virtual ICollection<Aplicacao> Aplicacoes { get; set; } = new List<Aplicacao>();
}

/// <summary>
/// Histórico de mudanças de status de aplicações
/// </summary>
public class HistoricoStatusAplicacao
{
    public int Id { get; set; }
    
    public int AplicacaoId { get; set; }
    public int StatusAnteriorId { get; set; }
    public int StatusNovoId { get; set; }
    
    public DateTime DataMudanca { get; set; } = DateTime.UtcNow;
    public int? AlteradoPorId { get; set; }
    
    [MaxLength(500)]
    public string? Motivo { get; set; }
    
    [MaxLength(1000)]
    public string? Observacoes { get; set; }
    
    /// <summary>
    /// Se a mudança foi automática (por sistema) ou manual (por usuário)
    /// </summary>
    public bool MudancaAutomatica { get; set; } = false;
    
    // Relacionamentos
    public virtual Aplicacao Aplicacao { get; set; } = null!;
    public virtual StatusAplicacao StatusAnterior { get; set; } = null!;
    public virtual StatusAplicacao StatusNovo { get; set; } = null!;
    public virtual Usuario? AlteradoPor { get; set; }
}