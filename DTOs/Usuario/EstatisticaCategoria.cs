using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Estatística por categoria
/// </summary>
public class EstatisticaCategoria
{
    public string Categoria { get; set; } = string.Empty;
    public int Total { get; set; }
}