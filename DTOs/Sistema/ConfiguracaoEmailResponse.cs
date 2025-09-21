using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Sistema;

public class ConfiguracaoEmailResponse
{
    public int Id { get; set; }
    public string ServidorSmtp { get; set; } = string.Empty;
    public int Porta { get; set; }
    public string EmailRemetente { get; set; } = string.Empty;
    public string NomeRemetente { get; set; } = string.Empty;
    public bool UsarSsl { get; set; }
    public bool UsarTls { get; set; }
    public bool UsarAutenticacao { get; set; }
    public string? EmailResposta { get; set; }
    public string? EmailCopia { get; set; }
    
    public string? EmailCopiaOculta { get; set; }
    
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public List<TemplateEmailResponse> Templates { get; set; } = new();
}