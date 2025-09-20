namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Permissão não utilizada
/// </summary>
public class PermissaoNaoUtilizada
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public DateTime DataCriacao { get; set; }
    public int DiasDesdeUltimoUso { get; set; }
}