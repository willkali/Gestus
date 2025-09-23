namespace Gestus.DTOs.Usuario;

/// <summary>
/// Aplicação disponível para solicitação de acesso pelo usuário
/// </summary>
public class AplicacaoDisponivelUsuario
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
    /// Código da aplicação
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Descrição da aplicação
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// URL para acessar a aplicação
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
    /// Nível de segurança necessário
    /// </summary>
    public int NivelSeguranca { get; set; }

    /// <summary>
    /// Se requer aprovação para acesso
    /// </summary>
    public bool RequerAprovacao { get; set; }

    /// <summary>
    /// Se permite auto-registro
    /// </summary>
    public bool PermiteAutoRegistro { get; set; }

    /// <summary>
    /// Status do usuário em relação a esta aplicação
    /// </summary>
    public StatusUsuarioAplicacao StatusUsuario { get; set; } = null!;

    /// <summary>
    /// Instruções específicas para solicitar acesso
    /// </summary>
    public string? InstrucoesSolicitacao { get; set; }

    /// <summary>
    /// Estimativa de tempo para aprovação
    /// </summary>
    public string? TempoAprovacaoEstimado { get; set; }

    /// <summary>
    /// Lista de responsáveis pela aprovação
    /// </summary>
    public List<string> ResponsaveisAprovacao { get; set; } = new();
}

/// <summary>
/// Status do usuário em relação à aplicação
/// </summary>
public class StatusUsuarioAplicacao
{
    public string Codigo { get; set; } = string.Empty; // "sem_acesso", "pendente", "aprovado", "rejeitado", "expirado"
    public string Nome { get; set; } = string.Empty;
    public string CorFundo { get; set; } = string.Empty;
    public string CorTexto { get; set; } = string.Empty;
    public string? Icone { get; set; }
    public bool PodeSolicitar { get; set; }
    public bool PodeAcessar { get; set; }
    public string? Motivo { get; set; } // Motivo por não poder solicitar/acessar
}