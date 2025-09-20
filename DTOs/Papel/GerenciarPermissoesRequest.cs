using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Papel;

/// <summary>
/// Dados para gerenciamento de permissões de um papel
/// </summary>
public class GerenciarPermissoesRequest
{
    /// <summary>
    /// Operação a ser realizada
    /// Valores: "substituir", "adicionar", "remover", "limpar"
    /// </summary>
    [Required(ErrorMessage = "Operação é obrigatória")]
    [RegularExpression("^(substituir|adicionar|remover|limpar)$", 
        ErrorMessage = "Operação deve ser: substituir, adicionar, remover ou limpar")]
    public string Operacao { get; set; } = string.Empty;

    /// <summary>
    /// Lista de permissões para a operação
    /// Obrigatório para: substituir, adicionar, remover
    /// Ignorado para: limpar
    /// </summary>
    public List<string>? Permissoes { get; set; }

    /// <summary>
    /// Observações sobre a alteração
    /// </summary>
    [StringLength(300, ErrorMessage = "Observações devem ter no máximo 300 caracteres")]
    public string? Observacoes { get; set; }
}