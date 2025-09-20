namespace Gestus.DTOs.Grupo;

/// <summary>
/// Resumo de um grupo para listagens
/// </summary>
public class GrupoResumo
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    
    /// <summary>
    /// Total de usuários no grupo
    /// </summary>
    public int TotalUsuarios { get; set; }
    
    /// <summary>
    /// Total de usuários ativos no grupo
    /// </summary>
    public int UsuariosAtivos { get; set; }
    
    /// <summary>
    /// Últimos usuários adicionados (primeiros 3 nomes)
    /// </summary>
    public List<string> UltimosUsuarios { get; set; } = new();
}