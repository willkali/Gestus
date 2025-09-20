using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Request para gerenciar usuários de um grupo
/// </summary>
public class GerenciarUsuariosGrupoRequest
{
    /// <summary>
    /// Operação a ser executada: adicionar, remover, substituir, limpar
    /// </summary>
    [Required(ErrorMessage = "Operação é obrigatória")]
    public string Operacao { get; set; } = string.Empty;

    /// <summary>
    /// Lista de IDs dos usuários para a operação
    /// </summary>
    public List<int> UsuariosIds { get; set; } = new();
}