using System.ComponentModel.DataAnnotations;

namespace Gestus.Modelos;

public class Permissao
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Recurso { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Acao { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Categoria { get; set; }

    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Relacionamentos
    public virtual ICollection<PapelPermissao> PapelPermissoes { get; set; } = new List<PapelPermissao>();
}