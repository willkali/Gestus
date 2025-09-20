using System.ComponentModel.DataAnnotations;

namespace Gestus.Modelos;

public class RegistroAuditoria
{
    public int Id { get; set; }

    public int? UsuarioId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Acao { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Recurso { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? RecursoId { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    public DateTime DataHora { get; set; } = DateTime.UtcNow;

    [MaxLength(45)]
    public string? EnderecoIp { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public string? DadosAntes { get; set; }
    public string? DadosDepois { get; set; }

    // Relacionamento
    public virtual Usuario? Usuario { get; set; }
}