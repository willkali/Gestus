using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Request para aprovar/rejeitar acesso de usuário à aplicação
/// </summary>
public class AprovarAcessoAplicacaoRequest
{
    /// <summary>
    /// Decisão da aprovação: "aprovar", "rejeitar", "aprovar_temporario"
    /// </summary>
    [Required(ErrorMessage = "Decisão é obrigatória")]
    public string Decisao { get; set; } = string.Empty;

    /// <summary>
    /// Observações sobre a decisão
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Observações devem ter no máximo 1000 caracteres")]
    public string? Observacoes { get; set; }

    /// <summary>
    /// Data de expiração do acesso (se aprovado)
    /// </summary>
    public DateTime? DataExpiracao { get; set; }

    /// <summary>
    /// Configurações específicas para o usuário na aplicação
    /// </summary>
    public string? ConfiguracoesUsuario { get; set; }

    /// <summary>
    /// Notificar usuário da decisão por email
    /// </summary>
    public bool NotificarUsuario { get; set; } = true;

    /// <summary>
    /// Para aprovação temporária - duração em dias
    /// </summary>
    [Range(1, 365, ErrorMessage = "Duração temporária deve ser entre 1 e 365 dias")]
    public int? DuracaoTemporariaDias { get; set; }

    /// <summary>
    /// Permissões específicas a conceder (opcional)
    /// </summary>
    public List<int>? PermissoesEspecificas { get; set; }
}

/// <summary>
/// Resposta da aprovação de acesso
/// </summary>
public class RespostaAprovacaoAcesso
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string DecisaoTomada { get; set; } = string.Empty;
    public UsuarioResumo Usuario { get; set; } = new();
    public AplicacaoUsuario AplicacaoAfetada { get; set; } = new();
    public DateTime? DataExpiracao { get; set; }
    public bool NotificacaoEnviada { get; set; }
    public string? ProximaAcaoNecessaria { get; set; }
}