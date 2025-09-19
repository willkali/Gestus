namespace Gestus.DTOs.Usuario;

/// <summary>
/// Permissão completa com informações de origem
/// </summary>
public class PermissaoCompleta
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Recurso { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public string? OrigemPapel { get; set; }
}