using FluentValidation;
using Gestus.DTOs.Grupo;
using Gestus.Dados;
using Microsoft.EntityFrameworkCore;

namespace Gestus.Validadores;

public class OperacaoLoteGruposValidator : AbstractValidator<OperacaoLoteGruposRequest>
{
    private readonly GestusDbContexto _context;

    public OperacaoLoteGruposValidator(GestusDbContexto context)
    {
        _context = context;

        RuleFor(x => x.Operacao)
            .NotEmpty().WithMessage("Operação é obrigatória")
            .Must(operacao => new[] { "ativar", "desativar", "excluir", "alterar-tipo" }
                .Contains(operacao.ToLower()))
            .WithMessage("Operação deve ser: ativar, desativar, excluir ou alterar-tipo");

        RuleFor(x => x.GruposIds)
            .NotEmpty().WithMessage("Lista de grupos é obrigatória")
            .Must(ids => ids.Count >= 1 && ids.Count <= 50)
            .WithMessage("Deve especificar entre 1 e 50 grupos");

        RuleForEach(x => x.GruposIds)
            .GreaterThan(0).WithMessage("ID do grupo deve ser maior que zero")
            .MustAsync(GrupoExiste).WithMessage("Grupo com ID {PropertyValue} não existe");

        When(x => x.Operacao.ToLower() == "alterar-tipo", () =>
        {
            RuleFor(x => x.NovoTipo)
                .NotEmpty().WithMessage("Novo tipo é obrigatório para operação alterar-tipo")
                .MaximumLength(50).WithMessage("Tipo deve ter no máximo 50 caracteres");
        });
    }

    private async Task<bool> GrupoExiste(int grupoId, CancellationToken cancellationToken)
    {
        return await _context.Grupos
            .AnyAsync(g => g.Id == grupoId, cancellationToken);
    }
}