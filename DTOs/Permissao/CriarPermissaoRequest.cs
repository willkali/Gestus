using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Permissao;

/// <summary>
/// Request para criação de nova permissão
/// </summary>
public class CriarPermissaoRequest
{
    /// <summary>
    /// Nome único da permissão (formato: Recurso.Acao)
    /// </summary>
    [Required(ErrorMessage = "Nome da permissão é obrigatório")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 100 caracteres")]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Descrição detalhada da permissão
    /// </summary>
    [Required(ErrorMessage = "Descrição da permissão é obrigatória")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Descrição deve ter entre 5 e 200 caracteres")]
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Recurso que a permissão controla
    /// </summary>
    [Required(ErrorMessage = "Recurso é obrigatório")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Recurso deve ter entre 2 e 50 caracteres")]
    public string Recurso { get; set; } = string.Empty;

    /// <summary>
    /// Ação que a permissão permite
    /// </summary>
    [Required(ErrorMessage = "Ação é obrigatória")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Ação deve ter entre 2 e 50 caracteres")]
    public string Acao { get; set; } = string.Empty;

    /// <summary>
    /// Categoria da permissão (opcional)
    /// </summary>
    [StringLength(100, ErrorMessage = "Categoria deve ter no máximo 100 caracteres")]
    public string? Categoria { get; set; }
}