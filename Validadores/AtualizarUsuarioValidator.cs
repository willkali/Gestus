using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Gestus.Modelos;
using Gestus.DTOs.Usuario;

namespace Gestus.Validadores;

/// <summary>
/// Validador FluentValidation para atualização de usuário
/// </summary>
public class AtualizarUsuarioValidator : AbstractValidator<AtualizarUsuarioRequest>
{
    private readonly UserManager<Usuario> _userManager;

    // ✅ REMOVER: parâmetro int usuarioId do construtor
    public AtualizarUsuarioValidator(UserManager<Usuario> userManager)
    {
        _userManager = userManager;

        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Email deve ter formato válido")
                .MaximumLength(256).WithMessage("Email deve ter no máximo 256 caracteres");
                // ✅ REMOVER: validação de unicidade aqui (mover para o controller)
        });

        When(x => !string.IsNullOrEmpty(x.Nome), () =>
        {
            RuleFor(x => x.Nome)
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres")
                .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("Nome deve conter apenas letras e espaços");
        });

        When(x => !string.IsNullOrEmpty(x.Sobrenome), () =>
        {
            RuleFor(x => x.Sobrenome)
                .MaximumLength(150).WithMessage("Sobrenome deve ter no máximo 150 caracteres")
                .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("Sobrenome deve conter apenas letras e espaços");
        });

        When(x => !string.IsNullOrEmpty(x.Telefone), () =>
        {
            RuleFor(x => x.Telefone)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Telefone deve ter formato válido");
        });

        When(x => !string.IsNullOrEmpty(x.NovaSenha), () =>
        {
            RuleFor(x => x.NovaSenha)
                .MinimumLength(6).WithMessage("Nova senha deve ter pelo menos 6 caracteres")
                .Must(SenhaDeveSerSegura).WithMessage("Nova senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número");

            RuleFor(x => x.ConfirmarNovaSenha)
                .Equal(x => x.NovaSenha).WithMessage("Confirmação de nova senha não confere");
        });

        RuleFor(x => x.Observacoes)
            .MaximumLength(500).WithMessage("Observações devem ter no máximo 500 caracteres");
    }

    // ✅ REMOVER: método EmailDeveSerUnicoParaOutroUsuario (mover lógica para controller)

    private bool SenhaDeveSerSegura(string senha)
    {
        return senha.Any(char.IsUpper) && 
               senha.Any(char.IsLower) && 
               senha.Any(char.IsDigit);
    }
}