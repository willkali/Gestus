using FluentValidation;
using Gestus.DTOs.Grupo;

namespace Gestus.Validadores;

public class BuscaAvancadaGruposValidator : AbstractValidator<BuscaAvancadaGruposRequest>
{
    public BuscaAvancadaGruposValidator()
    {
        RuleFor(x => x.Pagina)
            .GreaterThan(0).WithMessage("Página deve ser maior que zero");

        RuleFor(x => x.ItensPorPagina)
            .GreaterThan(0).WithMessage("Itens por página deve ser maior que zero")
            .LessThanOrEqualTo(100).WithMessage("Máximo de 100 itens por página");

        When(x => x.MinUsuarios.HasValue && x.MaxUsuarios.HasValue, () =>
        {
            RuleFor(x => x.MinUsuarios!.Value)
                .LessThanOrEqualTo(x => x.MaxUsuarios!.Value)
                .WithMessage("Mínimo de usuários deve ser menor ou igual ao máximo");
        });

        When(x => x.DataCriacaoInicio.HasValue && x.DataCriacaoFim.HasValue, () =>
        {
            RuleFor(x => x.DataCriacaoInicio!.Value)
                .LessThanOrEqualTo(x => x.DataCriacaoFim!.Value)
                .WithMessage("Data início deve ser anterior ou igual à data fim");
        });

        When(x => x.Tipos?.Any() == true, () =>
        {
            RuleFor(x => x.Tipos!)
                .Must(tipos => tipos.Count <= 10)
                .WithMessage("Máximo de 10 tipos por busca");
        });
    }
}