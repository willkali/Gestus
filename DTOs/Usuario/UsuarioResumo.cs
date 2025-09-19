namespace Gestus.DTOs.Usuario;

/// <summary>
/// Usuário resumido para listagens
/// </summary>
public class UsuarioResumo
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Sobrenome { get; set; } = string.Empty;
    public string NomeCompleto { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? UltimoLogin { get; set; }
    public List<string> Papeis { get; set; } = new();
    public int TotalPermissoes { get; set; }
}