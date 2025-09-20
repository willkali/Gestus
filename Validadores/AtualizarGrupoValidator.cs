using FluentValidation;
using Gestus.DTOs.Grupo;
using Gestus.Dados;
using Microsoft.EntityFrameworkCore;

namespace Gestus.Validadores;

public class AtualizarGrupoValidator : AbstractValidator<AtualizarGrupoRequest>
{
    private readonly GestusDbContexto _context;

    public AtualizarGrupoValidator(GestusDbContexto context)
    {
        _context = context;

        When(x => !string.IsNullOrEmpty(x.Nome), () =>
        {
            RuleFor(x => x.Nome!)
                .Length(3, 100).WithMessage("Nome deve ter entre 3 e 100 caracteres");
        });

        When(x => !string.IsNullOrEmpty(x.Descricao), () =>
        {
            RuleFor(x => x.Descricao!)
                .Length(10, 200).WithMessage("Descrição deve ter entre 10 e 200 caracteres");
        });

        When(x => !string.IsNullOrEmpty(x.Tipo), () =>
        {
            RuleFor(x => x.Tipo!)
                .MaximumLength(50).WithMessage("Tipo deve ter no máximo 50 caracteres");
        });
    }
}