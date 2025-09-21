using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Sistema;

/// <summary>
/// Variável obrigatória faltante
/// </summary>
public class VariavelFaltante
{
    public string Nome { get; set; } = string.Empty;
    public string Chave { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? ExemploValor { get; set; }
}