namespace Gestus.DTOs.Permissao;

/// <summary>
/// Estatísticas de uso de uma permissão
/// </summary>
public class EstatisticasPermissao
{
    /// <summary>
    /// Total de papéis ativos que possuem a permissão
    /// </summary>
    public int TotalPapeisAtivos { get; set; }

    /// <summary>
    /// Total de papéis inativos que possuem a permissão
    /// </summary>
    public int TotalPapeisInativos { get; set; }

    /// <summary>
    /// Total de usuários que possuem a permissão (através dos papéis)
    /// </summary>
    public int TotalUsuariosComPermissao { get; set; }

    /// <summary>
    /// Data da última atribuição da permissão a um papel
    /// </summary>
    public DateTime? UltimaAtribuicao { get; set; }
}