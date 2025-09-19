using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Estatísticas do usuário na busca
/// </summary>
public class EstatisticasBusca
{
    public int TotalPapeis { get; set; }
    public int TotalGrupos { get; set; }
    public int TotalPermissoes { get; set; }
}