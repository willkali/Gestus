using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Aplicacao;

/// <summary>
/// Dados para criação de uma nova aplicação
/// </summary>
public class CriarAplicacaoRequest
{
    /// <summary>
    /// Nome da aplicação
    /// </summary>
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Código único da aplicação
    /// </summary>
    [Required(ErrorMessage = "Código é obrigatório")]
    [MaxLength(50, ErrorMessage = "Código deve ter no máximo 50 caracteres")]
    [RegularExpression(@"^[a-z0-9-_]+$", ErrorMessage = "Código deve conter apenas letras minúsculas, números, hífens e underscores")]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Descrição da aplicação
    /// </summary>
    [Required(ErrorMessage = "Descrição é obrigatória")]
    [MaxLength(200, ErrorMessage = "Descrição deve ter no máximo 200 caracteres")]
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// URL base da aplicação
    /// </summary>
    [MaxLength(500, ErrorMessage = "URL base deve ter no máximo 500 caracteres")]
    [Url(ErrorMessage = "URL base deve ter formato válido")]
    public string? UrlBase { get; set; }

    /// <summary>
    /// ID do tipo de aplicação
    /// </summary>
    [Required(ErrorMessage = "Tipo de aplicação é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "Tipo de aplicação deve ser válido")]
    public int TipoAplicacaoId { get; set; }

    /// <summary>
    /// ID do status da aplicação (opcional, padrão: Ativa)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Status da aplicação deve ser válido")]
    public int? StatusAplicacaoId { get; set; }

    /// <summary>
    /// Versão da aplicação
    /// </summary>
    [MaxLength(50, ErrorMessage = "Versão deve ter no máximo 50 caracteres")]
    public string? Versao { get; set; }

    /// <summary>
    /// Client ID para OAuth (opcional)
    /// </summary>
    [MaxLength(100, ErrorMessage = "Client ID deve ter no máximo 100 caracteres")]
    public string? ClientId { get; set; }

    /// <summary>
    /// Client Secret encriptado (opcional)
    /// </summary>
    [MaxLength(500, ErrorMessage = "Client Secret deve ter no máximo 500 caracteres")]
    public string? ClientSecretEncriptado { get; set; }

    /// <summary>
    /// URLs de redirecionamento (JSON array)
    /// </summary>
    public string? UrlsRedirecionamento { get; set; }

    /// <summary>
    /// Scopes permitidos (JSON array)
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
    [Range(1, 10, ErrorMessage = "Nível de segurança deve estar entre 1 e 10")]
    public int NivelSeguranca { get; set; } = 5;

    /// <summary>
    /// Requer aprovação para acesso
    /// </summary>
    public bool RequerAprovacao { get; set; } = false;

    /// <summary>
    /// Permite auto-registro de usuários
    /// </summary>
    public bool PermiteAutoRegistro { get; set; } = false;

    /// <summary>
    /// Observações administrativas
    /// </summary>
    [MaxLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres")]
    public string? Observacoes { get; set; }
}