namespace Gestus.DTOs.Notificacao;

/// <summary>
/// DTO para retornar dados da notificação
/// </summary>
public class NotificacaoDTO
{
    public Guid Id { get; set; }
    public int UsuarioId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? Icone { get; set; }
    public string Cor { get; set; } = "info";
    public bool Lida { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataLeitura { get; set; }
    public DateTime? DataExpiracao { get; set; }
    public string? Origem { get; set; }
    public int Prioridade { get; set; }
    public bool EnviarEmail { get; set; }
    public bool EmailEnviado { get; set; }
    public string? DadosAdicionais { get; set; }
    public string IdadeTexto { get; set; } = string.Empty;
}
