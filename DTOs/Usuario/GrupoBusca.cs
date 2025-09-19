using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Grupo na busca
/// </summary>
public class GrupoBusca
{
    public string Nome { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public DateTime DataAdesao { get; set; }
}