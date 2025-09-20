namespace Gestus.DTOs.Papel;

/// <summary>
/// Dados completos de um papel
/// </summary>
public class PapelCompleto
{
    /// <summary>
    /// ID único do papel
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nome do papel
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Descrição detalhada do papel
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Categoria do papel
    /// </summary>
    public string? Categoria { get; set; }

    /// <summary>
    /// Nível hierárquico do papel
    /// </summary>
    public int Nivel { get; set; }

    /// <summary>
    /// Status ativo do papel
    /// </summary>
    public bool Ativo { get; set; }

    /// <summary>
    /// Data de criação do papel
    /// </summary>
    public DateTime DataCriacao { get; set; }

    /// <summary>
    /// Data da última atualização
    /// </summary>
    public DateTime? DataAtualizacao { get; set; }

    /// <summary>
    /// Lista completa de permissões do papel
    /// </summary>
    public List<PermissaoPapel> Permissoes { get; set; } = new();

    /// <summary>
    /// Total de usuários com este papel
    /// </summary>
    public int TotalUsuarios { get; set; }

    /// <summary>
    /// Estatísticas detalhadas do papel
    /// </summary>
    public EstatisticasPapel Estatisticas { get; set; } = new();

    /// <summary>
    /// Indica se é um papel do sistema
    /// </summary>
    public bool PapelSistema => new[] { "SuperAdmin", "Admin", "Usuario" }.Contains(Nome);

    /// <summary>
    /// Indica se o papel pode ser excluído
    /// </summary>
    public bool PodeExcluir => !PapelSistema && TotalUsuarios == 0;

    /// <summary>
    /// Indica se o papel pode ser editado
    /// </summary>
    public bool PodeEditar => Nome != "SuperAdmin";
}