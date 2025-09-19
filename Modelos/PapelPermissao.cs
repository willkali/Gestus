namespace Gestus.Modelos;

public class PapelPermissao
{
    public int PapelId { get; set; }
    public int PermissaoId { get; set; }
    
    public DateTime DataAtribuicao { get; set; } = DateTime.UtcNow;
    public bool Ativo { get; set; } = true;

    // Relacionamentos de navegação
    public virtual Papel Papel { get; set; } = null!;
    public virtual Permissao Permissao { get; set; } = null!;
}