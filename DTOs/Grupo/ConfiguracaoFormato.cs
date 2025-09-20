using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Configurações específicas por formato de exportação
/// </summary>
public class ConfiguracaoFormato
{
    /// <summary>
    /// Para CSV: separador de campos
    /// </summary>
    public string? SeparadorCSV { get; set; } = ",";

    /// <summary>
    /// Para Excel: nome da planilha
    /// </summary>
    public string? NomePlanilha { get; set; } = "Grupos";

    /// <summary>
    /// Para PDF: incluir gráficos
    /// </summary>
    public bool IncluirGraficosPDF { get; set; } = false;

    /// <summary>
    /// Encoding do arquivo
    /// </summary>
    public string? Encoding { get; set; } = "UTF-8";
}