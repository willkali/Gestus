using FluentValidation;
using Gestus.DTOs.Auditoria;

namespace Gestus.Validadores;

public class FiltrosAuditoriaValidator : AbstractValidator<FiltrosAuditoria>
{
    public FiltrosAuditoriaValidator()
    {
        // Validações herdadas de FiltrosBase
        RuleFor(x => x.Pagina)
            .GreaterThan(0).WithMessage("Página deve ser maior que zero");

        RuleFor(x => x.ItensPorPagina)
            .InclusiveBetween(1, 100).WithMessage("Itens por página deve estar entre 1 e 100");

        RuleFor(x => x.DirecaoOrdenacao)
            .Must(direcao => string.IsNullOrEmpty(direcao) || 
                           new[] { "asc", "desc" }.Contains(direcao.ToLower()))
            .WithMessage("Direção de ordenação deve ser 'asc' ou 'desc'");

        // Validações específicas de auditoria
        RuleFor(x => x.UsuarioId)
            .GreaterThan(0).WithMessage("ID do usuário deve ser maior que zero")
            .When(x => x.UsuarioId.HasValue);

        RuleFor(x => x.NomeUsuario)
            .MaximumLength(100).WithMessage("Nome do usuário deve ter no máximo 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.NomeUsuario));

        RuleFor(x => x.EmailUsuario)
            .EmailAddress().WithMessage("Email deve ter formato válido")
            .MaximumLength(100).WithMessage("Email deve ter no máximo 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.EmailUsuario));

        RuleFor(x => x.Acao)
            .MaximumLength(100).WithMessage("Ação deve ter no máximo 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Acao));

        RuleFor(x => x.Recurso)
            .MaximumLength(100).WithMessage("Recurso deve ter no máximo 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Recurso));

        RuleFor(x => x.RecursoId)
            .MaximumLength(50).WithMessage("ID do recurso deve ter no máximo 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.RecursoId));

        // Validação de período
        RuleFor(x => x.DataFim)
            .GreaterThan(x => x.DataInicio)
            .WithMessage("Data fim deve ser posterior à data início")
            .When(x => x.DataInicio.HasValue && x.DataFim.HasValue);

        RuleFor(x => x.DataInicio)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Data início não pode ser futura")
            .When(x => x.DataInicio.HasValue);

        // Validação de período máximo (1 ano)
        RuleFor(x => x)
            .Must(ValidarPeriodoMaximo)
            .WithMessage("Período de consulta não pode exceder 1 ano")
            .When(x => x.DataInicio.HasValue && x.DataFim.HasValue);

        RuleFor(x => x.EnderecoIp)
            .Must(ValidarFormatoIp).WithMessage("Formato de IP inválido")
            .When(x => !string.IsNullOrEmpty(x.EnderecoIp));

        RuleFor(x => x.BuscaObservacoes)
            .MaximumLength(500).WithMessage("Busca em observações deve ter no máximo 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.BuscaObservacoes));

        RuleFor(x => x.CategoriasAcao)
            .Must(categorias => categorias == null || categorias.Count <= 10)
            .WithMessage("Máximo de 10 categorias de ação permitidas");

        RuleForEach(x => x.CategoriasAcao)
            .MaximumLength(50).WithMessage("Categoria de ação deve ter no máximo 50 caracteres")
            .When(x => x.CategoriasAcao != null);

        // Validação de ordenação específica para auditoria
        RuleFor(x => x.OrdenarPor)
            .Must(campo => string.IsNullOrEmpty(campo) || 
                          new[] { "datahora", "acao", "recurso", "usuario", "ip" }
                          .Contains(campo.ToLower()))
            .WithMessage("Campo de ordenação deve ser: datahora, acao, recurso, usuario ou ip")
            .When(x => !string.IsNullOrEmpty(x.OrdenarPor));
    }

    private bool ValidarPeriodoMaximo(FiltrosAuditoria filtros)
    {
        if (!filtros.DataInicio.HasValue || !filtros.DataFim.HasValue)
            return true;

        var periodo = filtros.DataFim.Value - filtros.DataInicio.Value;
        return periodo.TotalDays <= 365;
    }

    private bool ValidarFormatoIp(string? ip)
    {
        if (string.IsNullOrEmpty(ip))
            return true;

        // Validação básica de IPv4 e IPv6
        return System.Net.IPAddress.TryParse(ip, out _);
    }
}