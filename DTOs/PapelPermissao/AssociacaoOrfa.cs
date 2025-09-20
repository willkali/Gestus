namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Associação órfã
/// </summary>
public class AssociacaoOrfa
{
    public int PapelId { get; set; }
    public string PapelNome { get; set; } = string.Empty;
    public int PermissaoId { get; set; }
    public string PermissaoNome { get; set; } = string.Empty;
    public string TipoProblema { get; set; } = string.Empty;
    public DateTime DataAtribuicao { get; set; }
}