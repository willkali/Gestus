using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Request para exportação de grupos
/// </summary>
public class ExportarGruposRequest
{
    /// <summary>
    /// Formato de exportação: csv, xlsx, json, pdf
    /// </summary>
    [Required(ErrorMessage = "Formato é obrigatório")]
    [RegularExpression("^(csv|xlsx|json|pdf)$", ErrorMessage = "Formato deve ser: csv, xlsx, json ou pdf")]
    public string Formato { get; set; } = "csv";

    /// <summary>
    /// Incluir detalhes dos usuários
    /// </summary>
    public bool IncluirDetalhes { get; set; } = false;

    /// <summary>
    /// Incluir estatísticas nos dados
    /// </summary>
    public bool IncluirEstatisticas { get; set; } = true;

    /// <summary>
    /// Filtros a aplicar na exportação
    /// </summary>
    public FiltrosGrupo? Filtros { get; set; }

    /// <summary>
    /// Campos específicos para exportar
    /// </summary>
    public List<string>? CamposEspecificos { get; set; }

    /// <summary>
    /// Configurações específicas do formato
    /// </summary>
    public ConfiguracaoFormato? ConfiguracaoFormato { get; set; }
}