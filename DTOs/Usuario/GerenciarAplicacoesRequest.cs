using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Request para gerenciamento de aplicações do usuário
/// </summary>
public class GerenciarAplicacoesRequest
{
    /// <summary>
    /// Tipo de operação: "adicionar", "remover", "substituir", "aprovar", "rejeitar", "suspender", "reativar"
    /// </summary>
    [Required(ErrorMessage = "Operação é obrigatória")]
    public string Operacao { get; set; } = string.Empty;

    /// <summary>
    /// Lista de IDs das aplicações para a operação
    /// </summary>
    public List<int> AplicacoesIds { get; set; } = new();

    /// <summary>
    /// Justificativa para a operação
    /// </summary>
    [MaxLength(500, ErrorMessage = "Justificativa deve ter no máximo 500 caracteres")]
    public string? Justificativa { get; set; }

    /// <summary>
    /// Observações administrativas sobre a operação
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Observações devem ter no máximo 1000 caracteres")]
    public string? Observacoes { get; set; }

    /// <summary>
    /// Data de expiração para novos acessos (opcional)
    /// </summary>
    public DateTime? DataExpiracao { get; set; }

    /// <summary>
    /// Configurações específicas para as aplicações (JSON)
    /// </summary>
    public string? ConfiguracoesUsuario { get; set; }

    /// <summary>
    /// Se deve enviar notificação por email ao usuário
    /// </summary>
    public bool NotificarUsuario { get; set; } = true;

    /// <summary>
    /// Se deve aprovar automaticamente (para operação "adicionar")
    /// </summary>
    public bool AprovarAutomaticamente { get; set; } = false;
}

/// <summary>
/// Resposta do gerenciamento de aplicações
/// </summary>
public class RespostaGerenciamentoAplicacoes
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string TipoOperacao { get; set; } = string.Empty;
    public UsuarioResumo Usuario { get; set; } = new();
    public List<AplicacaoUsuario> AplicacoesAtuais { get; set; } = new();
    public EstatisticasOperacaoAplicacoes Estatisticas { get; set; } = new();
    public List<string> Alertas { get; set; } = new();
    public List<string> Erros { get; set; } = new();
}

/// <summary>
/// Estatísticas da operação de aplicações
/// </summary>
public class EstatisticasOperacaoAplicacoes
{
    public int TotalAplicacoesAntes { get; set; }
    public int TotalAplicacoesDepois { get; set; }
    public int AplicacoesAdicionadas { get; set; }
    public int AplicacoesRemovidas { get; set; }
    public int AplicacoesAprovadas { get; set; }
    public int AplicacoesRejeitadas { get; set; }
    public int TotalPermissoes { get; set; }
    public List<string> NomesAplicacoesAdicionadas { get; set; } = new();
    public List<string> NomesAplicacoesRemovidas { get; set; } = new();
}