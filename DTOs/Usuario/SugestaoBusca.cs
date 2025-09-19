using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Sugestão para busca
/// </summary>
public class SugestaoBusca
{
    public string Tipo { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Detalhes { get; set; }
}