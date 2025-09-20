using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Request para atualização de grupo existente
/// </summary>
public class AtualizarGrupoRequest
{
    /// <summary>
    /// Nome do grupo (opcional para atualização)
    /// </summary>
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 100 caracteres")]
    public string? Nome { get; set; }

    /// <summary>
    /// Descrição do grupo (opcional para atualização)
    /// </summary>
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Descrição deve ter entre 10 e 200 caracteres")]
    public string? Descricao { get; set; }

    /// <summary>
    /// Tipo do grupo (opcional para atualização)
    /// </summary>
    [StringLength(50, ErrorMessage = "Tipo deve ter no máximo 50 caracteres")]
    public string? Tipo { get; set; }

    /// <summary>
    /// Status ativo do grupo (opcional para atualização)
    /// </summary>
    public bool? Ativo { get; set; }
}