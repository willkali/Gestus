using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Gestus.Modelos;
using Gestus.DTOs.Usuario;

namespace Gestus.Validadores;

/// <summary>
/// Validador para gerenciamento de papéis
/// </summary>
public class GerenciarPapeisValidator : AbstractValidator<GerenciarPapeisRequest>
{
    private readonly RoleManager<Papel> _roleManager;

    public GerenciarPapeisValidator(RoleManager<Papel> roleManager)
    {
        _roleManager = roleManager;

        RuleFor(x => x.Operacao)
            .NotEmpty().WithMessage("Operação é obrigatória")
            .Must(OperacaoValida).WithMessage("Operação deve ser: substituir, adicionar, remover ou limpar");

        When(x => x.Operacao?.ToLower() != "limpar", () =>
        {
            RuleFor(x => x.Papeis)
                .NotEmpty().WithMessage("Lista de papéis é obrigatória para esta operação")
                .Must(PapeisNaoVazios).WithMessage("Lista de papéis não pode conter valores vazios");
        });

        RuleFor(x => x.Observacoes)
            .MaximumLength(500).WithMessage("Observações devem ter no máximo 500 caracteres");
    }

    private bool OperacaoValida(string operacao)
    {
        if (string.IsNullOrEmpty(operacao)) return false;
        
        var operacoesValidas = new[] { "substituir", "adicionar", "remover", "limpar" };
        return operacoesValidas.Contains(operacao.ToLower());
    }

    private bool PapeisNaoVazios(List<string> papeis)
    {
        return papeis.All(p => !string.IsNullOrWhiteSpace(p));
    }
}