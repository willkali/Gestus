using FluentValidation;
using Gestus.DTOs.Usuario;

namespace Gestus.Validadores;

/// <summary>
/// Validador para busca avançada
/// </summary>
public class BuscaAvancadaValidator : AbstractValidator<SolicitacaoBuscaAvancada>
{
    public BuscaAvancadaValidator()
    {
        RuleFor(x => x.TextoGeral)
            .MaximumLength(200).WithMessage("Texto geral deve ter no máximo 200 caracteres");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email deve ter formato válido")
            .MaximumLength(256).WithMessage("Email deve ter no máximo 256 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Nome)
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres");

        RuleFor(x => x.Telefone)
            .MaximumLength(20).WithMessage("Telefone deve ter no máximo 20 caracteres");

        RuleFor(x => x.DataCriacaoFim)
            .GreaterThanOrEqualTo(x => x.DataCriacaoInicio)
            .WithMessage("Data fim deve ser maior ou igual à data início")
            .When(x => x.DataCriacaoInicio.HasValue && x.DataCriacaoFim.HasValue);

        RuleFor(x => x.UltimoLoginFim)
            .GreaterThanOrEqualTo(x => x.UltimoLoginInicio)
            .WithMessage("Data fim do último login deve ser maior ou igual à data início")
            .When(x => x.UltimoLoginInicio.HasValue && x.UltimoLoginFim.HasValue);

        RuleFor(x => x.OperadorPapeis)
            .Must(op => op == "E" || op == "OU")
            .WithMessage("Operador de papéis deve ser 'E' ou 'OU'");

        RuleFor(x => x.OperadorGrupos)
            .Must(op => op == "E" || op == "OU")
            .WithMessage("Operador de grupos deve ser 'E' ou 'OU'");

        RuleFor(x => x.OperadorPermissoes)
            .Must(op => op == "E" || op == "OU")
            .WithMessage("Operador de permissões deve ser 'E' ou 'OU'");

        RuleFor(x => x.Pagina)
            .GreaterThan(0).WithMessage("Página deve ser maior que 0");

        RuleFor(x => x.ItensPorPagina)
            .InclusiveBetween(1, 100).WithMessage("Itens por página deve ser entre 1 e 100");

        RuleForEach(x => x.Ordenacao)
            .SetValidator(new CriterioOrdenacaoValidator())
            .When(x => x.Ordenacao != null);

        // ✅ VALIDAÇÃO COMPLEXA: Pelo menos um critério deve ser fornecido
        RuleFor(x => x)
            .Must(TemPeloMenosUmCriterio)
            .WithMessage("Pelo menos um critério de busca deve ser fornecido");
    }

    private bool TemPeloMenosUmCriterio(SolicitacaoBuscaAvancada request)
    {
        return !string.IsNullOrEmpty(request.TextoGeral) ||
               !string.IsNullOrEmpty(request.Email) ||
               !string.IsNullOrEmpty(request.Nome) ||
               !string.IsNullOrEmpty(request.Telefone) ||
               request.Ativo.HasValue ||
               request.EmailConfirmado.HasValue ||
               request.TelefoneConfirmado.HasValue ||
               request.DataCriacaoInicio.HasValue ||
               request.DataCriacaoFim.HasValue ||
               request.UltimoLoginInicio.HasValue ||
               request.UltimoLoginFim.HasValue ||
               request.Papeis?.Any() == true ||
               request.Grupos?.Any() == true ||
               request.Permissoes?.Any() == true ||
               request.SemPapeis == true ||
               request.SemGrupos == true ||
               request.SemUltimoLogin == true;
    }
}

/// <summary>
/// Validador para critério de ordenação
/// </summary>
public class CriterioOrdenacaoValidator : AbstractValidator<CriterioOrdenacao>
{
    public CriterioOrdenacaoValidator()
    {
        RuleFor(x => x.Campo)
            .NotEmpty().WithMessage("Campo de ordenação é obrigatório")
            .Must(CampoValido).WithMessage("Campo de ordenação inválido");

        RuleFor(x => x.Direcao)
            .Must(d => string.IsNullOrEmpty(d) || d.ToLower() == "asc" || d.ToLower() == "desc")
            .WithMessage("Direção deve ser 'asc' ou 'desc'");
    }

    private bool CampoValido(string campo)
    {
        if (string.IsNullOrEmpty(campo)) return false;

        var camposValidos = new[]
        {
            "email", "nome", "datacriacao", "ultimologin", "ativo",
            "totalpapeis", "totalgrupos", "emailconfirmado", "telefoneconfirmado"
        };

        return camposValidos.Contains(campo.ToLower());
    }
}