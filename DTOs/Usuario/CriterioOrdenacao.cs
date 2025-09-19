using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Critério de ordenação para busca avançada
/// </summary>
public class CriterioOrdenacao
{
    /// <summary>
    /// Campo para ordenação
    /// </summary>
    [Required(ErrorMessage = "Campo é obrigatório")]
    public string Campo { get; set; } = string.Empty;

    /// <summary>
    /// Direção da ordenação: "asc" ou "desc"
    /// </summary>
    public string? Direcao { get; set; } = "asc";
}