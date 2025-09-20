namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Distribuição por categoria
/// </summary>
public class DistribuicaoCategoria
{
    public string Categoria { get; set; } = string.Empty;
    public int TotalAssociacoes { get; set; }
    public int TotalPermissoes { get; set; }
    public int TotalPapeis { get; set; }
}