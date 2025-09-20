namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Distribuições
/// </summary>
public class Distribuicoes
{
    public Dictionary<string, int> PorCategoria { get; set; } = new();
    public Dictionary<int, int> PorNivelPapel { get; set; } = new();
}