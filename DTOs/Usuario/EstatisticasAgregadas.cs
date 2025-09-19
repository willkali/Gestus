using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Estatísticas agregadas dos resultados
/// </summary>
public class EstatisticasAgregadas
{
    public int TotalUsuarios { get; set; }
    public int UsuariosAtivos { get; set; }
    public int UsuariosInativos { get; set; }
    public int UsuariosComEmail { get; set; }
    public int UsuariosComTelefone { get; set; }
    public int UsuariosSemUltimoLogin { get; set; }
    public List<EstatisticaCategoria> DistribuicaoPorPapel { get; set; } = new();
    public List<EstatisticaCategoria> DistribuicaoPorGrupo { get; set; } = new();
}