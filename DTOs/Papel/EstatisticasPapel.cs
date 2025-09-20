namespace Gestus.DTOs.Papel;

/// <summary>
/// Estatísticas detalhadas de um papel
/// </summary>
public class EstatisticasPapel
{
    /// <summary>
    /// Total de permissões ativas no papel
    /// </summary>
    public int TotalPermissoes { get; set; }

    /// <summary>
    /// Total de usuários com o papel
    /// </summary>
    public int TotalUsuarios { get; set; }

    /// <summary>
    /// Usuários ativos com o papel
    /// </summary>
    public int UsuariosAtivos { get; set; }

    /// <summary>
    /// Usuários inativos com o papel
    /// </summary>
    public int UsuariosInativos { get; set; }

    /// <summary>
    /// Data da última atribuição do papel a um usuário
    /// </summary>
    public DateTime? UltimaAtribuicao { get; set; }

    /// <summary>
    /// Distribuição de permissões por categoria
    /// </summary>
    public Dictionary<string, int> PermissoesPorCategoria { get; set; } = new();

    /// <summary>
    /// Percentual de uso em relação ao total de usuários ativos
    /// </summary>
    public decimal? PercentualUso { get; set; }

    /// <summary>
    /// Indica se o papel está sendo usado ativamente
    /// </summary>
    public bool EmUso => UsuariosAtivos > 0;

    /// <summary>
    /// Classificação do papel baseado no número de permissões
    /// </summary>
    public string ClassificacaoPapel => TotalPermissoes switch
    {
        >= 20 => "Administrador",
        >= 10 => "Gestor",
        >= 5 => "Operador",
        _ => "Básico"
    };
}