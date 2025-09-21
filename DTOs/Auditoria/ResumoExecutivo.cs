namespace Gestus.DTOs.Auditoria;

/// <summary>
/// Resumo executivo do relatório
/// </summary>
public class ResumoExecutivo
{
    public int TotalUsuariosAtivos { get; set; }
    public int TotalRecursosAfetados { get; set; }
    public string AcaoMaisFrequente { get; set; } = string.Empty;
    public string RecursoMaisAlterado { get; set; } = string.Empty;
    public string HorarioPicoAtividade { get; set; } = string.Empty;
    public List<string> AlertasCompliance { get; set; } = new();
    public List<string> RecomendacoesSecurity { get; set; } = new();
}