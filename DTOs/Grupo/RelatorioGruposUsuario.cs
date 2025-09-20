namespace Gestus.DTOs.Grupo;

/// <summary>
/// Relatório de grupos de um usuário específico
/// </summary>
public class RelatorioGruposUsuario
{
    /// <summary>
    /// Informações do usuário
    /// </summary>
    public dynamic Usuario { get; set; } = new { };

    /// <summary>
    /// Lista de grupos do usuário
    /// </summary>
    public List<GrupoDoUsuario> Grupos { get; set; } = new();

    /// <summary>
    /// Estatísticas dos grupos do usuário
    /// </summary>
    public EstatisticasGruposUsuario Estatisticas { get; set; } = new();

    /// <summary>
    /// Data de geração do relatório
    /// </summary>
    public DateTime DataGeracao { get; set; } = DateTime.UtcNow;
}