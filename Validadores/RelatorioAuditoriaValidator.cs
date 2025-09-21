using FluentValidation;
using Gestus.DTOs.Auditoria;

namespace Gestus.Validadores;

public class RelatorioAuditoriaValidator : AbstractValidator<RelatorioAuditoriaRequest>
{
    public RelatorioAuditoriaValidator()
    {
        RuleFor(x => x.TipoRelatorio)
            .NotEmpty().WithMessage("Tipo de relatório é obrigatório")
            .Must(tipo => new[] { "atividade-usuario", "historico-recurso", "timeline", "compliance", "dashboard" }
                .Contains(tipo.ToLower()))
            .WithMessage("Tipo de relatório deve ser: atividade-usuario, historico-recurso, timeline, compliance ou dashboard");

        RuleFor(x => x.DataInicio)
            .NotEmpty().WithMessage("Data início é obrigatória")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Data início não pode ser futura");

        RuleFor(x => x.DataFim)
            .NotEmpty().WithMessage("Data fim é obrigatória")
            .GreaterThan(x => x.DataInicio)
            .WithMessage("Data fim deve ser posterior à data início")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Data fim não pode ser futura");

        // Validação de período máximo baseado no tipo de relatório
        RuleFor(x => x)
            .Must(ValidarPeriodoMaximoPorTipo)
            .WithMessage("Período excede o limite permitido para este tipo de relatório");

        RuleFor(x => x.Formato)
            .NotEmpty().WithMessage("Formato é obrigatório")
            .Must(formato => new[] { "json", "csv", "excel", "pdf" }
                .Contains(formato.ToLower()))
            .WithMessage("Formato deve ser: json, csv, excel ou pdf");

        RuleFor(x => x.AgruparPor)
            .Must(agrupamento => string.IsNullOrEmpty(agrupamento) || 
                               new[] { "usuario", "recurso", "acao", "data", "ip" }
                               .Contains(agrupamento.ToLower()))
            .WithMessage("Agrupamento deve ser: usuario, recurso, acao, data ou ip")
            .When(x => !string.IsNullOrEmpty(x.AgruparPor));

        // Validação específica para cada tipo de relatório
        When(x => x.TipoRelatorio.ToLower() == "atividade-usuario", () =>
        {
            RuleFor(x => x.Filtros)
                .NotNull().WithMessage("Filtros são obrigatórios para relatório de atividade por usuário");

            RuleFor(x => x.Filtros!.UsuarioId)
                .NotEmpty().WithMessage("ID do usuário é obrigatório para relatório de atividade")
                .When(x => x.Filtros != null);
        });

        When(x => x.TipoRelatorio.ToLower() == "historico-recurso", () =>
        {
            RuleFor(x => x.Filtros)
                .NotNull().WithMessage("Filtros são obrigatórios para relatório de histórico de recurso");

            RuleFor(x => x.Filtros!.Recurso)
                .NotEmpty().WithMessage("Tipo de recurso é obrigatório para relatório de histórico")
                .When(x => x.Filtros != null);
        });

        When(x => x.Formato.ToLower() == "pdf", () =>
        {
            RuleFor(x => x)
                .Must(x => (x.DataFim - x.DataInicio).TotalDays <= 90)
                .WithMessage("Relatórios em PDF são limitados a 90 dias de período");
        });
    }

    private bool ValidarPeriodoMaximoPorTipo(RelatorioAuditoriaRequest request)
    {
        var periodo = request.DataFim - request.DataInicio;
        
        return request.TipoRelatorio.ToLower() switch
        {
            "timeline" => periodo.TotalDays <= 30,     // Timeline: máximo 30 dias
            "dashboard" => periodo.TotalDays <= 7,     // Dashboard: máximo 7 dias
            "compliance" => periodo.TotalDays <= 365,  // Compliance: máximo 1 ano
            _ => periodo.TotalDays <= 180              // Outros: máximo 6 meses
        };
    }
}