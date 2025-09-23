using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Request para usuário solicitar acesso a uma aplicação
/// </summary>
public class SolicitarAcessoAplicacaoRequest
{
    /// <summary>
    /// ID da aplicação solicitada
    /// </summary>
    [Required(ErrorMessage = "ID da aplicação é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "ID da aplicação deve ser válido")]
    public int AplicacaoId { get; set; }

    /// <summary>
    /// Justificativa para solicitação de acesso
    /// </summary>
    [Required(ErrorMessage = "Justificativa é obrigatória")]
    [MaxLength(500, ErrorMessage = "Justificativa deve ter no máximo 500 caracteres")]
    public string Justificativa { get; set; } = string.Empty;

    /// <summary>
    /// Data de expiração desejada (opcional)
    /// </summary>
    public DateTime? DataExpiracaoDesejada { get; set; }

    /// <summary>
    /// Departamento ou área que solicita o acesso
    /// </summary>
    [MaxLength(100, ErrorMessage = "Departamento deve ter no máximo 100 caracteres")]
    public string? Departamento { get; set; }

    /// <summary>
    /// Supervisor que autorizou a solicitação
    /// </summary>
    [MaxLength(200, ErrorMessage = "Supervisor deve ter no máximo 200 caracteres")]
    public string? SupervisorAutorizador { get; set; }

    /// <summary>
    /// Configurações iniciais desejadas (JSON)
    /// </summary>
    public string? ConfiguracoesDesejadas { get; set; }

    /// <summary>
    /// Se aceita acesso temporário enquanto aguarda aprovação definitiva
    /// </summary>
    public bool AceitaAcessoTemporario { get; set; } = false;
}

/// <summary>
/// Resposta da solicitação de acesso
/// </summary>
public class RespostaSolicitacaoAcesso
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public int SolicitacaoId { get; set; }
    public string StatusSolicitacao { get; set; } = string.Empty; // "pendente", "aprovada", "rejeitada"
    public AplicacaoUsuario? AplicacaoSolicitada { get; set; }
    public bool RequerAprovacao { get; set; }
    public List<string> ProximosPassos { get; set; } = new();
    public DateTime? PrevisaoResposta { get; set; }
    public List<string> Responsaveis { get; set; } = new(); // Quem pode aprovar
}