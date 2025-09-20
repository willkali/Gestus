namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Comparação entre papéis
/// </summary>
public class ComparacaoPapeis
{
    /// <summary>
    /// Papel com mais permissões
    /// </summary>
    public string? PapelComMaisPermissoes { get; set; }

    /// <summary>
    /// Papel com menos permissões
    /// </summary>
    public string? PapelComMenosPermissoes { get; set; }

    /// <summary>
    /// Posição deste papel no ranking
    /// </summary>
    public int PosicaoRanking { get; set; }

    /// <summary>
    /// Média de permissões por papel
    /// </summary>
    public decimal MediaPermissoesPorPapel { get; set; }
}