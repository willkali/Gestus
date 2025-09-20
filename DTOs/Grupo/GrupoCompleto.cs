namespace Gestus.DTOs.Grupo;

/// <summary>
/// Detalhes completos de um grupo
/// </summary>
public class GrupoCompleto
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
    /// Estatísticas do grupo
    /// </summary>
    public EstatisticasGrupo Estatisticas { get; set; } = new();
    
    /// <summary>
    /// Lista de usuários do grupo
    /// </summary>
    public List<UsuarioGrupo> Usuarios { get; set; } = new();
}