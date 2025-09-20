using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Request para criação de novo grupo
/// </summary>
public class CriarGrupoRequest
{
    /// <summary>
    /// Nome do grupo (obrigatório)
    /// </summary>
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 100 caracteres")]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Descrição do grupo (obrigatório)
    /// </summary>
    [Required(ErrorMessage = "Descrição é obrigatória")]
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Descrição deve ter entre 10 e 200 caracteres")]
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do grupo (opcional)
    /// </summary>
    [StringLength(50, ErrorMessage = "Tipo deve ter no máximo 50 caracteres")]
    public string? Tipo { get; set; }

    /// <summary>
    /// Status ativo do grupo
    /// </summary>
    public bool Ativo { get; set; } = true;

    /// <summary>
    /// Lista de IDs de usuários para adicionar ao grupo (opcional)
    /// </summary>
    public List<int>? UsuariosIds { get; set; }
}