using FluentValidation;
using Gestus.DTOs.Papel;

namespace Gestus.Validadores;

/// <summary>
/// Validador FluentValidation para filtros de papel
/// </summary>
public class FiltrosPapelValidator : AbstractValidator<FiltrosPapel>
{
    public FiltrosPapelValidator()
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
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres");
        });

        // ✅ VALIDAÇÃO DA DESCRIÇÃO (OPCIONAL)
        When(x => !string.IsNullOrEmpty(x.Descricao), () =>
        {
            RuleFor(x => x.Descricao)
                .MaximumLength(200).WithMessage("Descrição deve ter no máximo 200 caracteres");
        });

        // ✅ VALIDAÇÃO DA CATEGORIA (OPCIONAL)
        When(x => !string.IsNullOrEmpty(x.Categoria), () =>
        {
            RuleFor(x => x.Categoria)
                .MaximumLength(100).WithMessage("Categoria deve ter no máximo 100 caracteres");
        });

        // ✅ VALIDAÇÃO DOS NÍVEIS (OPCIONAL)
        When(x => x.NivelMinimo.HasValue, () =>
        {
            RuleFor(x => x.NivelMinimo!.Value)
                .GreaterThan(0).WithMessage("Nível mínimo deve ser maior que zero")
                .LessThanOrEqualTo(999).WithMessage("Nível mínimo deve ser menor ou igual a 999");
        });

        When(x => x.NivelMaximo.HasValue, () =>
        {
            RuleFor(x => x.NivelMaximo!.Value)
                .GreaterThan(0).WithMessage("Nível máximo deve ser maior que zero")
                .LessThanOrEqualTo(999).WithMessage("Nível máximo deve ser menor ou igual a 999");
        });

        // ✅ VALIDAÇÃO DE CONSISTÊNCIA DOS NÍVEIS
        When(x => x.NivelMinimo.HasValue && x.NivelMaximo.HasValue, () =>
        {
            RuleFor(x => x.NivelMinimo!.Value)
                .LessThanOrEqualTo(x => x.NivelMaximo!.Value)
                .WithMessage("Nível mínimo deve ser menor ou igual ao nível máximo");
        });

        // ✅ VALIDAÇÃO DAS DATAS (OPCIONAL)
        When(x => x.DataCriacaoInicio.HasValue, () =>
        {
            RuleFor(x => x.DataCriacaoInicio!.Value)
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Data de criação início não pode ser futura");
        });

        When(x => x.DataCriacaoFim.HasValue, () =>
        {
            RuleFor(x => x.DataCriacaoFim!.Value)
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Data de criação fim não pode ser futura");
        });

        // ✅ VALIDAÇÃO DE CONSISTÊNCIA DAS DATAS
        When(x => x.DataCriacaoInicio.HasValue && x.DataCriacaoFim.HasValue, () =>
        {
            RuleFor(x => x.DataCriacaoInicio!.Value)
                .LessThanOrEqualTo(x => x.DataCriacaoFim!.Value)
                .WithMessage("Data de criação início deve ser anterior ou igual à data fim");
        });

        // ✅ VALIDAÇÃO DAS PERMISSÕES (OPCIONAL)
        When(x => x.Permissoes != null && x.Permissoes.Any(), () =>
        {
            RuleFor(x => x.Permissoes)
                .Must(list => list!.Count <= 20).WithMessage("Máximo de 20 permissões para filtro");

            RuleForEach(x => x.Permissoes)
                .NotEmpty().WithMessage("Nome da permissão não pode ser vazio")
                .MaximumLength(100).WithMessage("Nome da permissão deve ter no máximo 100 caracteres");
        });

        // ✅ VALIDAÇÃO DE ORDENAÇÃO (OPCIONAL)
        When(x => !string.IsNullOrEmpty(x.OrdenarPor), () =>
        {
            RuleFor(x => x.OrdenarPor!)
                .Must(OrdenacaoDeveSerValida).WithMessage("Campo de ordenação inválido");
        });

        When(x => !string.IsNullOrEmpty(x.DirecaoOrdenacao), () =>
        {
            RuleFor(x => x.DirecaoOrdenacao!)
                .Must(DirecaoDeveSerValida).WithMessage("Direção de ordenação deve ser 'asc' ou 'desc'");
        });

        // ✅ VALIDAÇÃO DE CONFLITOS LÓGICOS
        RuleFor(x => x)
            .Must(FiltrosNaoDevemConflitar).WithMessage("Não é possível filtrar apenas papéis do sistema E apenas personalizados simultaneamente");
    }

    /// <summary>
    /// Verifica se o campo de ordenação é válido
    /// </summary>
    private static bool OrdenacaoDeveSerValida(string ordenarPor)
    {
        var camposValidos = new[]
        {
            "nome", "descricao", "categoria", "nivel", "ativo", 
            "datacriacao", "totalusuarios", "totalpermissoes"
        };

        return camposValidos.Contains(ordenarPor.ToLower());
    }

    /// <summary>
    /// Verifica se a direção de ordenação é válida
    /// </summary>
    private static bool DirecaoDeveSerValida(string direcao)
    {
        return new[] { "asc", "desc" }.Contains(direcao.ToLower());
    }

    /// <summary>
    /// Verifica se não há conflitos lógicos nos filtros
    /// </summary>
    private bool FiltrosNaoDevemConflitar(FiltrosPapel filtros)
    {
        // Não pode filtrar APENAS sistema E APENAS personalizados ao mesmo tempo
        return !(filtros.ApenasRolesSistema && filtros.ApenasRolesPersonalizadas);
    }
}