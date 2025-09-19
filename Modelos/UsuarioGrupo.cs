namespace Gestus.Modelos;

public class UsuarioGrupo
{
    public int UsuarioId { get; set; }
    public int GrupoId { get; set; }
    
    public DateTime DataAdesao { get; set; } = DateTime.UtcNow;
    public bool Ativo { get; set; } = true;

    // Relacionamentos de navegação
    public virtual Usuario Usuario { get; set; } = null!;
    public virtual Grupo Grupo { get; set; } = null!;
}