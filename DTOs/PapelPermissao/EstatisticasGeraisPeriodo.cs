namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Estatísticas gerais do período
/// </summary>
public class EstatisticasGeraisPeriodo
{
    public int NovasAssociacoes { get; set; }
    public int AssociacoesAtivas { get; set; }
    public decimal TaxaCrescimento { get; set; }
}