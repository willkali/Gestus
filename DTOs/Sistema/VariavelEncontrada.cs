using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Sistema;

/// <summary>
/// Variável encontrada no template
/// </summary>
public class VariavelEncontrada
{
    public string Nome { get; set; } = string.Empty;
    public string Chave { get; set; } = string.Empty;
    public bool Obrigatoria { get; set; }
    public string? Descricao { get; set; }
}