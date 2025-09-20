namespace Gestus.DTOs.Grupo;

/// <summary>
/// Usuário do grupo com informações básicas
/// </summary>
public class UsuarioGrupo
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sobrenome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public DateTime DataAdesao { get; set; }
    public List<string> Papeis { get; set; } = new();
}