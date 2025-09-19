using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Request para atualização de usuário
/// </summary>
public class AtualizarUsuarioRequest
{
    [EmailAddress(ErrorMessage = "Email deve ter formato válido")]
    [MaxLength(256, ErrorMessage = "Email deve ter no máximo 256 caracteres")]
    public string? Email { get; set; }

    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string? Nome { get; set; }

    [MaxLength(150, ErrorMessage = "Sobrenome deve ter no máximo 150 caracteres")]
    public string? Sobrenome { get; set; }

    [Phone(ErrorMessage = "Telefone deve ter formato válido")]
    [MaxLength(20, ErrorMessage = "Telefone deve ter no máximo 20 caracteres")]
    public string? Telefone { get; set; }

    /// <summary>
    /// Senha atual (obrigatória para alteração da própria senha)
    /// </summary>
    public string? SenhaAtual { get; set; }

    /// <summary>
    /// Nova senha (opcional)
    /// </summary>
    [MinLength(6, ErrorMessage = "Nova senha deve ter pelo menos 6 caracteres")]
    public string? NovaSenha { get; set; }

    /// <summary>
    /// Confirmação da nova senha
    /// </summary>
    [Compare("NovaSenha", ErrorMessage = "Confirmação de senha não confere")]
    public string? ConfirmarNovaSenha { get; set; }

    /// <summary>
    /// Status ativo/inativo do usuário
    /// </summary>
    public bool? Ativo { get; set; }

    public bool ConfirmarEmailImediatamente { get; set; } = false;
    public bool ConfirmarTelefoneImediatamente { get; set; } = false;

    [MaxLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres")]
    public string? Observacoes { get; set; }

    /// <summary>
    /// Lista de papéis para atribuir ao usuário (substitui os existentes)
    /// </summary>
    public List<string>? Papeis { get; set; }
}