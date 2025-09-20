using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Permissao;

/// <summary>
/// Request para atualização de permissão existente
/// </summary>
public class AtualizarPermissaoRequest
{
    /// <summary>
    /// Novo nome da permissão (opcional)
    /// </summary>
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 100 caracteres")]
    public string? Nome { get; set; }

    /// <summary>
    /// Nova descrição da permissão (opcional)
    /// </summary>
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Descrição deve ter entre 5 e 200 caracteres")]
    public string? Descricao { get; set; }

    /// <summary>
    /// Novo recurso da permissão (opcional)
    /// </summary>
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Recurso deve ter entre 2 e 50 caracteres")]
    public string? Recurso { get; set; }

    /// <summary>
    /// Nova ação da permissão (opcional)
    /// </summary>
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Ação deve ter entre 2 e 50 caracteres")]
    public string? Acao { get; set; }

    /// <summary>
    /// Nova categoria da permissão (opcional)
    /// </summary>
    [StringLength(100, ErrorMessage = "Categoria deve ter no máximo 100 caracteres")]
    public string? Categoria { get; set; }

    /// <summary>
    /// Novo status ativo/inativo (opcional)
    /// </summary>
    public bool? Ativo { get; set; }
}