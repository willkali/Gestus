namespace Gestus.DTOs.Aplicacao;

/// <summary>
/// Versão resumida de aplicação para listagens
/// </summary>
public class AplicacaoResumo
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
    /// Informações do tipo de aplicação
    /// </summary>
    public TipoAplicacaoResumo TipoAplicacao { get; set; } = null!;

    /// <summary>
    /// Informações do status da aplicação
    /// </summary>
    public StatusAplicacaoResumo StatusAplicacao { get; set; } = null!;

    /// <summary>
    /// Versão da aplicação
    /// </summary>
    public string Versao { get; set; } = string.Empty;

    /// <summary>
    /// Nível de segurança (1-10)
    /// </summary>
    public int NivelSeguranca { get; set; }

    /// <summary>
    /// Indica se a aplicação está ativa
    /// </summary>
    public bool Ativa { get; set; }

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
    /// Nome de quem criou
    /// </summary>
    public string CriadoPor { get; set; } = string.Empty;
}