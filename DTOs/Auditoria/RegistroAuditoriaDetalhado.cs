namespace Gestus.DTOs.Auditoria;

/// <summary>
/// Registro de auditoria com informações detalhadas
/// </summary>
public class RegistroAuditoriaDetalhado
{
    public int Id { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string Recurso { get; set; } = string.Empty;
    public string? RecursoId { get; set; }
    public string? Observacoes { get; set; }
    public DateTime DataHora { get; set; }
    public string? EnderecoIp { get; set; }
    public string? UserAgent { get; set; }

    /// <summary>
    /// Informações do usuário que executou a ação
    /// </summary>
    public UsuarioAuditoria? Usuario { get; set; }

    /// <summary>
    /// Dados antes da alteração (JSON formatado)
    /// </summary>
    public object? DadosAntes { get; set; }

    /// <summary>
    /// Dados depois da alteração (JSON formatado)
    /// </summary>
    public object? DadosDepois { get; set; }

    /// <summary>
    /// Resumo das alterações principais
    /// </summary>
    public List<AlteracaoDetalhada> Alteracoes { get; set; } = new();

    /// <summary>
    /// Categoria da ação para agrupamento
    /// </summary>
    public string CategoriaAcao { get; set; } = string.Empty;

    /// <summary>
    /// Severidade da operação
    /// </summary>
    public string Severidade { get; set; } = "Normal";

    /// <summary>
    /// Contexto adicional da operação
    /// </summary>
    public Dictionary<string, object> Contexto { get; set; } = new();
}