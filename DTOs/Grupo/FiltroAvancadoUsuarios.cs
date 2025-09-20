using Gestus.DTOs.Comuns;
using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Filtros avançados para usuários dentro dos grupos
/// </summary>
public class FiltroAvancadoUsuarios
{
    /// <summary>
    /// Papéis que os usuários devem ter
    /// </summary>
    public List<string>? PapeisObrigatorios { get; set; }

    /// <summary>
    /// Papéis que os usuários NÃO devem ter
    /// </summary>
    public List<string>? PapeisExcluidos { get; set; }

    /// <summary>
    /// Filtrar apenas usuários ativos
    /// </summary>
    public bool ApenasUsuariosAtivos { get; set; } = true;
}