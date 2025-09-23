namespace Gestus.DTOs.Usuario;

/// <summary>
/// Informações de uma aplicação vinculada ao usuário
/// </summary>
public class AplicacaoUsuario
{
    /// <summary>
    /// ID da aplicação
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nome da aplicação
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Código único da aplicação
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Descrição da aplicação
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// URL base da aplicação
    /// </summary>
    public string? UrlBase { get; set; }

    /// <summary>
    /// Tipo da aplicação
    /// </summary>
    public TipoAplicacaoUsuario TipoAplicacao { get; set; } = null!;

    /// <summary>
    /// Status da aplicação
    /// </summary>
    public StatusAplicacaoUsuario StatusAplicacao { get; set; } = null!;

    /// <summary>
    /// Versão da aplicação
    /// </summary>
    public string Versao { get; set; } = string.Empty;

    /// <summary>
    /// Nível de segurança necessário (1-10)
    /// </summary>
    public int NivelSeguranca { get; set; }

    /// <summary>
    /// Status do acesso do usuário à aplicação
    /// </summary>
    public StatusAcessoUsuario StatusAcesso { get; set; } = null!;

    /// <summary>
    /// Data de solicitação de acesso
    /// </summary>
    public DateTime DataSolicitacao { get; set; }

    /// <summary>
    /// Data de aprovação do acesso
    /// </summary>
    public DateTime? DataAprovacao { get; set; }

    /// <summary>
    /// Data de expiração do acesso
    /// </summary>
    public DateTime? DataExpiracao { get; set; }

    /// <summary>
    /// Justificativa para solicitação de acesso
    /// </summary>
    public string? Justificativa { get; set; }

    /// <summary>
    /// Observações da aprovação
    /// </summary>
    public string? ObservacoesAprovacao { get; set; }

    /// <summary>
    /// Nome de quem aprovou o acesso
    /// </summary>
    public string? AprovadoPor { get; set; }

    /// <summary>
    /// Indica se o acesso está ativo
    /// </summary>
    public bool Ativo { get; set; }

    /// <summary>
    /// Indica se o acesso foi aprovado
    /// </summary>
    public bool Aprovado { get; set; }

    /// <summary>
    /// Configurações específicas do usuário na aplicação
    /// </summary>
    public string? ConfiguracoesUsuario { get; set; }

    /// <summary>
    /// Total de permissões do usuário nesta aplicação
    /// </summary>
    public int TotalPermissoes { get; set; }

    /// <summary>
    /// Permissões específicas do usuário na aplicação
    /// </summary>
    public List<PermissaoAplicacaoUsuario> Permissoes { get; set; } = new();
}

/// <summary>
/// Tipo de aplicação simplificado para usuário
/// </summary>
public class TipoAplicacaoUsuario
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Icone { get; set; }
    public string? Cor { get; set; }
}

/// <summary>
/// Status de aplicação simplificado para usuário
/// </summary>
public class StatusAplicacaoUsuario
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string CorFundo { get; set; } = string.Empty;
    public string CorTexto { get; set; } = string.Empty;
    public string? Icone { get; set; }
    public bool PermiteAcesso { get; set; }
    public string? MensagemUsuario { get; set; }
}

/// <summary>
/// Status do acesso do usuário à aplicação
/// </summary>
public class StatusAcessoUsuario
{
    public string Codigo { get; set; } = string.Empty; // "pendente", "aprovado", "negado", "expirado", "suspenso"
    public string Nome { get; set; } = string.Empty; // "Pendente", "Aprovado", "Negado", "Expirado", "Suspenso"
    public string CorFundo { get; set; } = string.Empty;
    public string CorTexto { get; set; } = string.Empty;
    public string? Icone { get; set; }
}

/// <summary>
/// Permissão específica do usuário na aplicação
/// </summary>
public class PermissaoAplicacaoUsuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Recurso { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public string OrigemPapel { get; set; } = string.Empty; // Nome do papel que concede esta permissão
    public DateTime DataAtribuicao { get; set; }
    public DateTime? DataExpiracao { get; set; }
}