using Microsoft.AspNetCore.Identity;

namespace Gestus.Modelos;

public class UsuarioPapel : IdentityUserRole<int>
{
    public DateTime DataAtribuicao { get; set; } = DateTime.UtcNow;
    public DateTime? DataExpiracao { get; set; }
    public bool Ativo { get; set; } = true;
    
    public int? AtribuidoPorId { get; set; }

    // Relacionamentos de navegação
    public virtual Usuario Usuario { get; set; } = null!;
    public virtual Papel Papel { get; set; } = null!;
    public virtual Usuario? AtribuidoPor { get; set; }
}