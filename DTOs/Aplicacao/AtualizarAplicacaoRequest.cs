using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Aplicacao;

/// <summary>
/// Dados para atualização de uma aplicação existente
/// </summary>
public class AtualizarAplicacaoRequest
{
    /// <summary>
    /// Nome da aplicação
    /// </summary>
    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string? Nome { get; set; }

    /// <summary>
    /// Descrição da aplicação
    /// </summary>
    [MaxLength(200, ErrorMessage = "Descrição deve ter no máximo 200 caracteres")]
    public string? Descricao { get; set; }

    /// <summary>
    /// URL base da aplicação
    /// </summary>
    [MaxLength(500, ErrorMessage = "URL base deve ter no máximo 500 caracteres")]
    [Url(ErrorMessage = "URL base deve ter formato válido")]
    public string? UrlBase { get; set; }

    /// <summary>
    /// ID do tipo de aplicação
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Tipo de aplicação deve ser válido")]
    public int? TipoAplicacaoId { get; set; }

    /// <summary>
    /// ID do status da aplicação
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Status da aplicação deve ser válido")]
    public int? StatusAplicacaoId { get; set; }

    /// <summary>
    /// Versão da aplicação
    /// </summary>
    [MaxLength(50, ErrorMessage = "Versão deve ter no máximo 50 caracteres")]
    public string? Versao { get; set; }

    /// <summary>
    /// Nível de segurança (1-10)
    /// </summary>
    [Range(1, 10, ErrorMessage = "Nível de segurança deve estar entre 1 e 10")]
    public int? NivelSeguranca { get; set; }

    /// <summary>
    /// Indica se a aplicação está ativa
    /// </summary>
    public bool? Ativa { get; set; }

    /// <summary>
    /// Requer aprovação para acesso
    /// </summary>
    public bool? RequerAprovacao { get; set; }

    /// <summary>
    /// Permite auto-registro de usuários
    /// </summary>
    public bool? PermiteAutoRegistro { get; set; }

    /// <summary>
    /// Observações administrativas
    /// </summary>
    [MaxLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres")]
    public string? Observacoes { get; set; }

    /// <summary>
    /// Motivo da mudança de status (se aplicável)
    /// </summary>
    [MaxLength(500, ErrorMessage = "Motivo deve ter no máximo 500 caracteres")]
    public string? MotivoMudancaStatus { get; set; }

    /// <summary>
    /// Observações sobre mudança de status (se aplicável)
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Observações da mudança devem ter no máximo 1000 caracteres")]
    public string? ObservacoesMudancaStatus { get; set; }
}