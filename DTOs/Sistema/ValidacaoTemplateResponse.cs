using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Sistema;

/// <summary>
/// Resposta da validação
/// </summary>
public class ValidacaoTemplateResponse
{
    public bool Valido { get; set; }
    public List<string> Erros { get; set; } = new();
    public List<string> Avisos { get; set; } = new();
    public List<VariavelEncontrada> VariaveisEncontradas { get; set; } = new();
    public List<VariavelFaltante> VariaveisFaltantes { get; set; } = new();
    public string? PreviewHtml { get; set; }
}