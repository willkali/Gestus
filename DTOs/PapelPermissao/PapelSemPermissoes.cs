namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Papel sem permissões
/// </summary>
public class PapelSemPermissoes
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public DateTime DataCriacao { get; set; }
    public int TotalUsuarios { get; set; }
}