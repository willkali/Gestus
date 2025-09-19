using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Gestus.Modelos;

public class Papel : IdentityRole<int>
{
    [Required]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Categoria { get; set; }

    public int Nivel { get; set; } = 1;
    public bool Ativo { get; set; } = true;
    
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }

    // Relacionamentos
    public virtual ICollection<UsuarioPapel> UsuarioPapeis { get; set; } = new List<UsuarioPapel>();
    public virtual ICollection<PapelPermissao> PapelPermissoes { get; set; } = new List<PapelPermissao>();
}