using FluentValidation;
using Gestus.DTOs.Auditoria;

namespace Gestus.Validadores;

/// <summary>
/// Validador para consultas específicas de auditoria
/// </summary>
public class ConsultaAuditoriaValidator : AbstractValidator<int>
{
    public ConsultaAuditoriaValidator()
    {
        RuleFor(id => id)
            .GreaterThan(0).WithMessage("ID do registro de auditoria deve ser maior que zero");
    }
}

/// <summary>
/// Request para busca avançada em auditoria
/// </summary>
public class BuscaAvancadaAuditoriaRequest
{
    public string? TextoBusca { get; set; }
    public List<string>? CamposBusca { get; set; }
    public FiltrosAuditoria? Filtros { get; set; }
    public bool BuscaExata { get; set; } = false;
    public bool IncluirDadosAntes { get; set; } = true;
    public bool IncluirDadosDepois { get; set; } = true;
}

/// <summary>
/// Validador para busca avançada
/// </summary>
public class BuscaAvancadaAuditoriaValidator : AbstractValidator<BuscaAvancadaAuditoriaRequest>
{
    public BuscaAvancadaAuditoriaValidator()
    {
        RuleFor(x => x.TextoBusca)
            .NotEmpty().WithMessage("Texto de busca é obrigatório")
            .MinimumLength(3).WithMessage("Texto de busca deve ter pelo menos 3 caracteres")
            .MaximumLength(500).WithMessage("Texto de busca deve ter no máximo 500 caracteres");

        RuleFor(x => x.CamposBusca)
            .Must(campos => campos == null || campos.Count <= 10)
            .WithMessage("Máximo de 10 campos de busca permitidos");

        RuleForEach(x => x.CamposBusca)
            .Must(campo => new[] { "acao", "recurso", "observacoes", "dadosantes", "dadosdepois", "ip", "useragent" }
                .Contains(campo.ToLower()))
            .WithMessage("Campo de busca inválido")
            .When(x => x.CamposBusca != null);

        RuleFor(x => x.Filtros)
            .SetValidator(new FiltrosAuditoriaValidator() as IValidator<FiltrosAuditoria?>)
            .When(x => x.Filtros != null);
    }
}