using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Request para operações em lote com grupos
/// </summary>
public class OperacaoLoteGruposRequest
{
    /// <summary>
    /// Operação a executar: ativar, desativar, excluir, alterar-tipo
    /// </summary>
    [Required(ErrorMessage = "Operação é obrigatória")]
    [RegularExpression("^(ativar|desativar|excluir|alterar-tipo)$", 
        ErrorMessage = "Operação deve ser: ativar, desativar, excluir ou alterar-tipo")]
    public string Operacao { get; set; } = string.Empty;

    /// <summary>
    /// IDs dos grupos para a operação
    /// </summary>
    [Required(ErrorMessage = "Lista de grupos é obrigatória")]
    [MinLength(1, ErrorMessage = "Deve especificar pelo menos um grupo")]
    public List<int> GruposIds { get; set; } = new();

    /// <summary>
    /// Para exclusão permanente
    /// </summary>
    public bool ExclusaoPermanente { get; set; } = false;

    /// <summary>
    /// Novo tipo (para operação alterar-tipo)
    /// </summary>
    public string? NovoTipo { get; set; }

    /// <summary>
    /// Observações da operação
    /// </summary>
    public string? Observacoes { get; set; }
}