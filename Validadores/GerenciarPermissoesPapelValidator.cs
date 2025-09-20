using FluentValidation;
using Gestus.DTOs.Papel;

namespace Gestus.Validadores;

/// <summary>
/// Validador FluentValidation para gerenciamento de permissões de papel
/// </summary>
public class GerenciarPermissoesPapelValidator : AbstractValidator<GerenciarPermissoesRequest>
{
    private readonly IServiceProvider _serviceProvider;

    public GerenciarPermissoesPapelValidator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // ✅ VALIDAÇÃO DA OPERAÇÃO
        RuleFor(x => x.Operacao)
            .NotEmpty().WithMessage("Operação é obrigatória")
            .Must(OperacaoDeveSerValida).WithMessage("Operação deve ser: substituir, adicionar, remover ou limpar");

        // ✅ VALIDAÇÃO DAS PERMISSÕES (CONDICIONAL)
        When(x => x.Operacao != "limpar", () =>
        {
            RuleFor(x => x.Permissoes)
                .NotEmpty().WithMessage("Lista de permissões é obrigatória para esta operação")
                .Must(PermissoesDevemSerValidas).WithMessage("Uma ou mais permissões são inválidas")
                .Must(list => list!.Count <= 50).WithMessage("Máximo de 50 permissões por operação");

            RuleForEach(x => x.Permissoes)
                .NotEmpty().WithMessage("Nome da permissão não pode ser vazio")
                .Matches(@"^[a-zA-Z0-9\.]+$").WithMessage("Permissão deve seguir o padrão: Recurso.Acao");
        });

        // ✅ VALIDAÇÃO ESPECIAL PARA "LIMPAR"
        When(x => x.Operacao == "limpar", () =>
        {
            RuleFor(x => x.Permissoes)
                .Must(list => list == null || !list.Any()).WithMessage("Lista de permissões deve estar vazia para operação 'limpar'");
        });

        // ✅ VALIDAÇÃO DAS OBSERVAÇÕES (OPCIONAL)
        When(x => !string.IsNullOrEmpty(x.Observacoes), () =>
        {
            RuleFor(x => x.Observacoes)
                .MaximumLength(300).WithMessage("Observações devem ter no máximo 300 caracteres");
        });
    }

    /// <summary>
    /// Verifica se a operação é válida
    /// </summary>
    private bool OperacaoDeveSerValida(string operacao)
    {
        var operacoesValidas = new[] { "substituir", "adicionar", "remover", "limpar" };
        return operacoesValidas.Contains(operacao.ToLower());
    }

    /// <summary>
    /// Verifica se todas as permissões fornecidas existem no sistema
    /// </summary>
    private bool PermissoesDevemSerValidas(List<string>? permissoes)
    {
        if (permissoes == null || !permissoes.Any())
            return true;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<Gestus.Dados.GestusDbContexto>();
            
            var permissoesExistentes = context.Permissoes
                .Where(p => p.Ativo)
                .Select(p => p.Nome)
                .ToHashSet();

            return permissoes.All(p => permissoesExistentes.Contains(p));
        }
        catch
        {
            return false;
        }
    }
}