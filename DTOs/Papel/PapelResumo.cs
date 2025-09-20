namespace Gestus.DTOs.Papel;

/// <summary>
/// Resumo de papel para listagens
/// </summary>
public class PapelResumo
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
    /// Descrição do papel
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
    /// Total de permissões associadas
    /// </summary>
    public int TotalPermissoes { get; set; }

    /// <summary>
    /// Total de usuários com este papel
    /// </summary>
    public int TotalUsuarios { get; set; }

    /// <summary>
    /// Resumo das principais permissões (primeiras 5)
    /// </summary>
    public List<string> PermissoesResumo { get; set; } = new();

    /// <summary>
    /// Indica se é um papel do sistema (não pode ser excluído)
    /// </summary>
    public bool PapelSistema => new[] { "SuperAdmin", "Admin", "Usuario" }.Contains(Nome);
}