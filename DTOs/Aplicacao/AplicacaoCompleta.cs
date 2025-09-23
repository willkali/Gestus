namespace Gestus.DTOs.Aplicacao;

/// <summary>
/// Informações completas de uma aplicação
/// </summary>
public class AplicacaoCompleta
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
    /// Informações completas do tipo de aplicação
    /// </summary>
    public TipoAplicacaoCompleto TipoAplicacao { get; set; } = null!;

    /// <summary>
    /// Informações completas do status da aplicação
    /// </summary>
    public StatusAplicacaoCompleto StatusAplicacao { get; set; } = null!;

    /// <summary>
    /// Versão da aplicação
    /// </summary>
    public string Versao { get; set; } = string.Empty;

    /// <summary>
    /// Client ID para OAuth
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// URLs de redirecionamento (JSON)
    /// </summary>
    public string? UrlsRedirecionamento { get; set; }

    /// <summary>
    /// Scopes permitidos (JSON)
    /// </summary>
    public string? ScopesPermitidos { get; set; }

    /// <summary>
    /// Configurações da aplicação (JSON)
    /// </summary>
    public string? Configuracoes { get; set; }

    /// <summary>
    /// Metadados específicos do tipo (JSON)
    /// </summary>
    public string? MetadadosTipo { get; set; }

    /// <summary>
    /// Nível de segurança (1-10)
    /// </summary>
    public int NivelSeguranca { get; set; }

    /// <summary>
    /// Indica se a aplicação está ativa
    /// </summary>
    public bool Ativa { get; set; }

    /// <summary>
    /// Requer aprovação para acesso
    /// </summary>
    public bool RequerAprovacao { get; set; }

    /// <summary>
    /// Permite auto-registro de usuários
    /// </summary>
    public bool PermiteAutoRegistro { get; set; }

    /// <summary>
    /// Observações administrativas
    /// </summary>
    public string? Observacoes { get; set; }

    /// <summary>
    /// Total de usuários com acesso
    /// </summary>
    public int TotalUsuarios { get; set; }

    /// <summary>
    /// Total de permissões configuradas
    /// </summary>
    public int TotalPermissoes { get; set; }

    /// <summary>
    /// Data de criação
    /// </summary>
    public DateTime DataCriacao { get; set; }

    /// <summary>
    /// Data de última atualização
    /// </summary>
    public DateTime? DataAtualizacao { get; set; }

    /// <summary>
    /// Nome de quem criou
    /// </summary>
    public string CriadoPor { get; set; } = string.Empty;

    /// <summary>
    /// Nome de quem atualizou
    /// </summary>
    public string? AtualizadoPor { get; set; }
}