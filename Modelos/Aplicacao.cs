using System.ComponentModel.DataAnnotations;

namespace Gestus.Modelos;

/// <summary>
/// Representa uma aplicação/sistema gerenciado pelo Gestus IAM
/// </summary>
public class Aplicacao
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty; // "Sistema Vendas", "API Produtos"
    
    [Required]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty; // "vendas", "produtos" (único)
    
    [Required]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? UrlBase { get; set; } // "https://vendas.empresa.com"
    
    [Required]
    [MaxLength(50)]
    public int TipoAplicacaoId { get; set; }
    public virtual TipoAplicacao TipoAplicacao { get; set; } = null!; // WebAPI, WebApp, Desktop, Mobile, CLI, etc

    [Required]
    public int StatusAplicacaoId { get; set; } = 1; // Default: Ativa
    public virtual StatusAplicacao StatusAplicacao { get; set; } = null!;
    
    [MaxLength(50)]
    public string Versao { get; set; } = "1.0.0";
    
    /// <summary>
    /// Client ID para integração OAuth/OpenIddict
    /// </summary>
    [MaxLength(100)]
    public string? ClientId { get; set; }
    
    /// <summary>
    /// Secret encriptado para autenticação
    /// </summary>
    [MaxLength(500)]
    public string? ClientSecretEncriptado { get; set; }
    
    /// <summary>
    /// URLs de redirecionamento permitidas (JSON array)
    /// </summary>
    public string? UrlsRedirecionamento { get; set; } = "[]";
    
    /// <summary>
    /// Scopes permitidos para esta aplicação (JSON array)
    /// </summary>
    public string? ScopesPermitidos { get; set; } = "[]";
    
    /// <summary>
    /// Configurações específicas da aplicação (JSON)
    /// Exemplo: {"timeout": 300, "maxUsuarios": 1000, "features": ["backup", "export"]}
    /// </summary>
    public string? Configuracoes { get; set; } = "{}";
    
    /// <summary>
    /// Metadados específicos por tipo de aplicação (JSON)
    /// WebAPI: {"endpoints": [...], "versaoApi": "v1"}
    /// Desktop: {"modulosDisponiveis": [...], "versaoMinima": "1.0"}
    /// Mobile: {"plataformas": ["Android", "iOS"], "versaoMinima": "1.0"}
    /// </summary>
    public string? MetadadosTipo { get; set; } = "{}";
    
    public bool Ativa { get; set; } = true;
    public bool RequerAprovacao { get; set; } = false; // Para acesso de usuários
    public bool PermiteAutoRegistro { get; set; } = false; // Usuários podem se auto-registrar

    public virtual ICollection<HistoricoStatusAplicacao> HistoricoStatus { get; set; } = new List<HistoricoStatusAplicacao>();
    
    /// <summary>
    /// Nível de segurança necessário (1-10)
    /// 1-3: Baixo, 4-6: Médio, 7-10: Alto
    /// </summary>
    public int NivelSeguranca { get; set; } = 5;
    
    [MaxLength(500)]
    public string? Observacoes { get; set; }
    
    public int CriadoPorId { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
    public int? AtualizadoPorId { get; set; }
    
    // Relacionamentos
    public virtual Usuario CriadoPor { get; set; } = null!;
    public virtual Usuario? AtualizadoPor { get; set; }
    public virtual ICollection<PermissaoAplicacao> Permissoes { get; set; } = new List<PermissaoAplicacao>();
    public virtual ICollection<UsuarioAplicacao> UsuariosAplicacao { get; set; } = new List<UsuarioAplicacao>();
}

/// <summary>
/// Permissões específicas de uma aplicação
/// </summary>
public class PermissaoAplicacao
{
    public int Id { get; set; }
    
    public int AplicacaoId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty; // "usuarios.criar", "relatorios.gerar"
    
    [Required]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Recurso { get; set; } = string.Empty; // "usuarios", "produtos", "dashboard"
    
    [Required]
    [MaxLength(50)]
    public string Acao { get; set; } = string.Empty; // "criar", "editar", "deletar", "visualizar"
    
    /// <summary>
    /// Para aplicações HTTP - endpoint específico
    /// </summary>
    [MaxLength(200)]
    public string? Endpoint { get; set; } // "/api/usuarios", "/dashboard/vendas"
    
    /// <summary>
    /// Para aplicações HTTP - método HTTP
    /// </summary>
    [MaxLength(20)]
    public string? MetodoHttp { get; set; } // "POST", "GET", "PUT", "DELETE", "*"
    
    /// <summary>
    /// Para aplicações Desktop/Compiladas - módulo interno
    /// </summary>
    [MaxLength(100)]
    public string? Modulo { get; set; } // "Vendas", "Relatórios", "Configurações"
    
    /// <summary>
    /// Para aplicações Desktop/Mobile - tela/formulário específico
    /// </summary>
    [MaxLength(100)]
    public string? Tela { get; set; } // "CadastroCliente", "ListaProdutos", "ConfiguracoesSistema"
    
    /// <summary>
    /// Para aplicações CLI - comando específico
    /// </summary>
    [MaxLength(100)]
    public string? Comando { get; set; } // "backup", "deploy", "migrate"
    
    /// <summary>
    /// Para aplicações de Banco - operação SQL
    /// </summary>
    [MaxLength(50)]
    public string? OperacaoSql { get; set; } // "SELECT", "INSERT", "UPDATE", "DELETE"
    
    /// <summary>
    /// Para aplicações de Banco - schema/database específico
    /// </summary>
    [MaxLength(100)]
    public string? Schema { get; set; } // "vendas", "produtos", "logs"
    
    /// <summary>
    /// Para aplicações de Banco - tabela/collection específica
    /// </summary>
    [MaxLength(100)]
    public string? Tabela { get; set; } // "usuarios", "pedidos", "auditoria"
    
    [MaxLength(100)]
    public string? Categoria { get; set; } // "CRUD", "Relatórios", "Configuração", "Segurança"
    
    /// <summary>
    /// Nível de privilégio necessário (1-10)
    /// </summary>
    public int Nivel { get; set; } = 1;
    
    /// <summary>
    /// Condições adicionais para a permissão (JSON)
    /// Exemplo: {"horarioPermitido": "08:00-18:00", "diasSemana": [1,2,3,4,5], "ipPermitidos": ["192.168.1.0/24"]}
    /// </summary>
    public string? Condicoes { get; set; } = "{}";
    
    public bool Ativa { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
    public int? AtualizadoPorId { get; set; }
    
    // Relacionamentos
    public virtual Aplicacao Aplicacao { get; set; } = null!;
    public virtual Usuario? AtualizadoPor { get; set; }
    public virtual ICollection<PapelPermissaoAplicacao> PapelPermissoes { get; set; } = new List<PapelPermissaoAplicacao>();
}

/// <summary>
/// Relacionamento Many-to-Many entre Papel e PermissaoAplicacao
/// </summary>
public class PapelPermissaoAplicacao
{
    public int PapelId { get; set; }
    public int PermissaoAplicacaoId { get; set; }
    public int AplicacaoId { get; set; } // Para facilitar consultas
    
    public DateTime DataAtribuicao { get; set; } = DateTime.UtcNow;
    public DateTime? DataExpiracao { get; set; }
    public bool Ativa { get; set; } = true;
    public int? AtribuidoPorId { get; set; }
    
    [MaxLength(500)]
    public string? Observacoes { get; set; }
    
    // Relacionamentos
    public virtual Papel Papel { get; set; } = null!;
    public virtual PermissaoAplicacao PermissaoAplicacao { get; set; } = null!;
    public virtual Aplicacao Aplicacao { get; set; } = null!;
    public virtual Usuario? AtribuidoPor { get; set; }
}

/// <summary>
/// Controle de acesso de usuários às aplicações
/// </summary>
public class UsuarioAplicacao
{
    public int UsuarioId { get; set; }
    public int AplicacaoId { get; set; }
    
    public bool Aprovado { get; set; } = false;
    public DateTime? DataAprovacao { get; set; }
    public int? AprovadoPorId { get; set; }
    
    public DateTime DataSolicitacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataExpiracao { get; set; }
    public bool Ativo { get; set; } = true;
    
    [MaxLength(500)]
    public string? Justificativa { get; set; }
    
    [MaxLength(500)]
    public string? ObservacoesAprovacao { get; set; }
    
    /// <summary>
    /// Configurações específicas do usuário na aplicação (JSON)
    /// Exemplo: {"tema": "dark", "idioma": "pt-BR", "permissoesTempestivas": [...]}
    /// </summary>
    public string? ConfiguracoesUsuario { get; set; } = "{}";
    
    // Relacionamentos
    public virtual Usuario Usuario { get; set; } = null!;
    public virtual Aplicacao Aplicacao { get; set; } = null!;
    public virtual Usuario? AprovadoPor { get; set; }
}