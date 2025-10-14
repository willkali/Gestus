using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Gestus.Modelos;
using Gestus.DTOs.Usuario;

namespace Gestus.Validadores;

/// <summary>
/// Validador FluentValidation para criação de usuário
/// </summary>
public class CriarUsuarioValidator : AbstractValidator<CriarUsuarioRequest>
{
    private readonly UserManager<Usuario> _userManager;

    public CriarUsuarioValidator(UserManager<Usuario> userManager)
    {
        _userManager = userManager;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email deve ter formato válido")
            .MaximumLength(256).WithMessage("Email deve ter no máximo 256 caracteres")
            .MustAsync(EmailDeveSerUnico).WithMessage("Já existe um usuário com este email");

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres")
            .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("Nome deve conter apenas letras e espaços");

        RuleFor(x => x.Sobrenome)
            .NotEmpty().WithMessage("Sobrenome é obrigatório")
            .MaximumLength(150).WithMessage("Sobrenome deve ter no máximo 150 caracteres")
            .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("Sobrenome deve conter apenas letras e espaços");

        RuleFor(x => x.Telefone)
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Telefone deve ter formato válido")
            .When(x => !string.IsNullOrEmpty(x.Telefone));

        // Senha é obrigatória apenas quando NÃO for gerar automaticamente
        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("Senha é obrigatória quando não gerar automaticamente")
            .MinimumLength(6).WithMessage("Senha deve ter pelo menos 6 caracteres")
            .Must(SenhaDeveSerSegura).WithMessage("Senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número")
            .When(x => !x.EnviarSenhaEmail); // Apenas quando não gerar automaticamente

        RuleFor(x => x.ConfirmarSenha)
            .Equal(x => x.Senha).WithMessage("Confirmação de senha não confere")
            .When(x => !x.EnviarSenhaEmail && !string.IsNullOrEmpty(x.Senha)); // Apenas quando senha foi fornecida

        RuleFor(x => x.Observacoes)
            .MaximumLength(500).WithMessage("Observações devem ter no máximo 500 caracteres");
    }

    private async Task<bool> EmailDeveSerUnico(string email, CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByEmailAsync(email);
        return usuario == null;
    }

    private bool SenhaDeveSerSegura(string senha)
    {
        return senha.Any(char.IsUpper) && 
               senha.Any(char.IsLower) && 
               senha.Any(char.IsDigit);
    }
}