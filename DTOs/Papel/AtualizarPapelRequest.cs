using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Papel;

/// <summary>
/// Dados para atualização de um papel existente
/// </summary>
public class AtualizarPapelRequest
{
    /// <summary>
    /// Novo nome do papel (opcional)
    /// </summary>
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 100 caracteres")]
    public string? Nome { get; set; }

    /// <summary>
    /// Nova descrição do papel (opcional)
    /// </summary>
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Descrição deve ter entre 5 e 200 caracteres")]
    public string? Descricao { get; set; }

    /// <summary>
    /// Nova categoria do papel (opcional)
    /// </summary>
    [StringLength(100, ErrorMessage = "Categoria deve ter no máximo 100 caracteres")]
    public string? Categoria { get; set; }

    /// <summary>
    /// Novo nível hierárquico (opcional)
    /// </summary>
    [Range(1, 999, ErrorMessage = "Nível deve estar entre 1 e 999")]
    public int? Nivel { get; set; }

    /// <summary>
    /// Novo status ativo/inativo (opcional)
    /// </summary>
    public bool? Ativo { get; set; }

    /// <summary>
    /// Nova lista de permissões (substitui todas as atuais se fornecida)
    /// </summary>
    public List<string>? Permissoes { get; set; }

    /// <summary>
    /// Observações sobre a alteração
    /// </summary>
    [StringLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres")]
    public string? Observacoes { get; set; }
}