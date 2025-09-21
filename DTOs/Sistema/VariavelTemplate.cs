using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Sistema;

/// <summary>
/// Informações da variável
/// </summary>
public class VariavelTemplate
{
    public string Nome { get; set; } = string.Empty;
    public string Chave { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? ExemploValor { get; set; }
    public bool Obrigatoria { get; set; }
}