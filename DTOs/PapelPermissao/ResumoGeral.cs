namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Resumo geral do sistema
/// </summary>
public class ResumoGeral
{
    public int TotalPapeis { get; set; }
    public int PapeisAtivos { get; set; }
    public int TotalPermissoes { get; set; }
    public int PermissoesAtivas { get; set; }
    public int TotalAssociacoes { get; set; }
    public int AssociacoesAtivas { get; set; }
    public DateTime DataGeracao { get; set; }
    public TimeSpan TempoProcessamento { get; set; }
}