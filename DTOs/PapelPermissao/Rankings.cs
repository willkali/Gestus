namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Rankings
/// </summary>
public class Rankings
{
    public Dictionary<string, int> TopPapeisPorPermissoes { get; set; } = new();
    public Dictionary<string, int> TopPermissoesPorPapeis { get; set; } = new();
}