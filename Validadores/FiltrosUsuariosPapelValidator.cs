using FluentValidation;
using Gestus.DTOs.Papel;

namespace Gestus.Validadores;

/// <summary>
/// Validador FluentValidation para filtros de usuários de papel
/// </summary>
public class FiltrosUsuariosPapelValidator : AbstractValidator<FiltrosUsuariosPapel>
{
    public FiltrosUsuariosPapelValidator()
    {
        // ✅ VALIDAÇÃO BÁSICA DE PAGINAÇÃO (herdada de FiltrosBase)
        RuleFor(x => x.Pagina)
            .GreaterThan(0).WithMessage("Página deve ser maior que zero");

        RuleFor(x => x.ItensPorPagina)
            .GreaterThan(0).WithMessage("Itens por página deve ser maior que zero")
            .LessThanOrEqualTo(100).WithMessage("Máximo de 100 itens por página");

        // ✅ VALIDAÇÃO DO NOME (OPCIONAL)
        When(x => !string.IsNullOrEmpty(x.Nome), () =>
        {
            RuleFor(x => x.Nome)
                .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres");
        });

        // ✅ VALIDAÇÃO DO EMAIL (OPCIONAL)
        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .MaximumLength(256).WithMessage("Email deve ter no máximo 256 caracteres")
                .EmailAddress().WithMessage("Email deve ter formato válido");
        });

        // ✅ VALIDAÇÃO DAS DATAS DE ATRIBUIÇÃO (OPCIONAL)
        When(x => x.DataAtribuicaoInicio.HasValue, () =>
        {
            RuleFor(x => x.DataAtribuicaoInicio!.Value)
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Data de atribuição início não pode ser futura");
        });

        When(x => x.DataAtribuicaoFim.HasValue, () =>
        {
            RuleFor(x => x.DataAtribuicaoFim!.Value)
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Data de atribuição fim não pode ser futura");
        });

        // ✅ VALIDAÇÃO DE CONSISTÊNCIA DAS DATAS
        When(x => x.DataAtribuicaoInicio.HasValue && x.DataAtribuicaoFim.HasValue, () =>
        {
            RuleFor(x => x.DataAtribuicaoInicio!.Value)
                .LessThanOrEqualTo(x => x.DataAtribuicaoFim!.Value)
                .WithMessage("Data de atribuição início deve ser anterior ou igual à data fim");
        });

        // ✅ VALIDAÇÃO DE ORDENAÇÃO (OPCIONAL)
        When(x => !string.IsNullOrEmpty(x.OrdenarPor), () =>
        {
            RuleFor(x => x.OrdenarPor)
                .Must(OrdenacaoDeveSerValida).WithMessage("Campo de ordenação inválido");
        });

        When(x => !string.IsNullOrEmpty(x.DirecaoOrdenacao), () =>
        {
            RuleFor(x => x.DirecaoOrdenacao)
                .Must(DirecaoDeveSerValida).WithMessage("Direção de ordenação deve ser 'asc' ou 'desc'");
        });
    }

    /// <summary>
    /// Verifica se o campo de ordenação é válido
    /// </summary>
    private bool OrdenacaoDeveSerValida(string? ordenarPor)
    {
        var camposValidos = new[]
        {
            "nome", "email", "ativo", "dataatribuicao", 
            "ultimologin", "totalpapeis", "nomecompleto"
        };

        return !string.IsNullOrEmpty(ordenarPor) && camposValidos.Contains(ordenarPor.ToLower());
    }

    /// <summary>
    /// Verifica se a direção de ordenação é válida
    /// </summary>
    private bool DirecaoDeveSerValida(string? direcao)
    {
        return !string.IsNullOrEmpty(direcao) && new[] { "asc", "desc" }.Contains(direcao.ToLower());
    }
}