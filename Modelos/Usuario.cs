using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Gestus.Modelos;

public class Usuario : IdentityUser<int>
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Sobrenome { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? NomeCompleto { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
    public DateTime? UltimoLogin { get; set; }
    
    public bool Ativo { get; set; } = true;
    
    [MaxLength(500)]
    public string? Observacoes { get; set; }

    // Relacionamentos
    public virtual ICollection<UsuarioPapel> UsuarioPapeis { get; set; } = new List<UsuarioPapel>();
    public virtual ICollection<UsuarioGrupo> UsuarioGrupos { get; set; } = new List<UsuarioGrupo>();
    public virtual ICollection<RegistroAuditoria> RegistrosAuditoria { get; set; } = new List<RegistroAuditoria>();
}