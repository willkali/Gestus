namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Resumo de limpeza
/// </summary>
public class ResumoLimpeza
{
    public int TotalAssociacoesComPapeisInativos { get; set; }
    public int TotalAssociacoesComPermissoesInativas { get; set; }
    public int TotalPermissoesNaoUtilizadas { get; set; }
    public int TotalPapeisSemPermissoes { get; set; }
    public bool LimpezaExecutada { get; set; }
    public int ItensLimpos { get; set; }
    public List<string> ErrosLimpeza { get; set; } = new();
    public DateTime DataAnalise { get; set; }
}