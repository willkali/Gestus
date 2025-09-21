using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Sistema;

/// <summary>
/// Tipos de template disponíveis
/// </summary>
public class TipoTemplateResponse
{
    public string Tipo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public List<VariavelTemplate> VariaveisObrigatorias { get; set; } = new();
    public List<VariavelTemplate> VariaveisOpcionais { get; set; } = new();
}