namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Comparações do sistema
/// </summary>
public class ComparacoesSistema
{
    public List<string> PapeisMaisComplexos { get; set; } = new();
    public List<string> PermissoesMaisUsadas { get; set; } = new();
}