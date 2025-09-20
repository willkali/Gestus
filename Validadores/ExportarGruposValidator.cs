using FluentValidation;
using Gestus.DTOs.Grupo;

namespace Gestus.Validadores;

public class ExportarGruposValidator : AbstractValidator<ExportarGruposRequest>
{
    public ExportarGruposValidator()
    {
        RuleFor(x => x.Formato)
            .NotEmpty().WithMessage("Formato é obrigatório")
            .Must(f => new[] { "csv", "xlsx", "json", "pdf" }.Contains(f.ToLower()))
            .WithMessage("Formato deve ser: csv, xlsx, json ou pdf");

        When(x => x.CamposEspecificos?.Any() == true, () =>
        {
            RuleFor(x => x.CamposEspecificos!)
                .Must(campos => campos.Count <= 20)
                .WithMessage("Máximo de 20 campos específicos");
        });
    }
}