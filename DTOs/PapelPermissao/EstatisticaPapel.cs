namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Estatística por papel
/// </summary>
public class EstatisticaPapel
{
    public int PapelId { get; set; }
    public string PapelNome { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public int TotalPermissoes { get; set; }
    public int TotalUsuarios { get; set; }
}