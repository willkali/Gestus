using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Gestus.Modelos;
using Gestus.DTOs.Papel;

namespace Gestus.Validadores;

/// <summary>
/// Validador FluentValidation para criação de papel
/// </summary>
public class CriarPapelValidator : AbstractValidator<CriarPapelRequest>
{
    private readonly RoleManager<Papel> _roleManager;
    private readonly IServiceProvider _serviceProvider;

    public CriarPapelValidator(RoleManager<Papel> roleManager, IServiceProvider serviceProvider)
    {
        _roleManager = roleManager;
        _serviceProvider = serviceProvider;

        // ✅ VALIDAÇÃO DO NOME - CORRIGIDO: Especificando explicitamente os tipos
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome do papel é obrigatório")
            .Length(2, 100).WithMessage("Nome deve ter entre 2 e 100 caracteres")
            .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Nome deve conter apenas letras, números, espaços, hífens e sublinhados")
            .MustAsync(async (string nome, CancellationToken token) => await NomeDeveSerUnico(nome, token))
            .WithMessage("Já existe um papel com este nome");

        // ✅ VALIDAÇÃO DA DESCRIÇÃO
        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição do papel é obrigatória")
            .Length(5, 200).WithMessage("Descrição deve ter entre 5 e 200 caracteres");

        // ✅ VALIDAÇÃO DA CATEGORIA (OPCIONAL)
        When(x => !string.IsNullOrEmpty(x.Categoria), () =>
        {
            RuleFor(x => x.Categoria)
                .MaximumLength(100).WithMessage("Categoria deve ter no máximo 100 caracteres")
                .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Categoria deve conter apenas letras, números, espaços, hífens e sublinhados");
        });

        // ✅ VALIDAÇÃO DO NÍVEL
        RuleFor(x => x.Nivel)
            .GreaterThan(0).WithMessage("Nível deve ser maior que zero")
            .LessThanOrEqualTo(999).WithMessage("Nível deve ser menor ou igual a 999");

        // ✅ VALIDAÇÃO DAS PERMISSÕES (OPCIONAL)
        When(x => x.Permissoes != null && x.Permissoes.Any(), () =>
        {
            RuleFor(x => x.Permissoes)
                .Must((List<string>? permissoes) => PermissoesDevemSerValidas(permissoes))
                .WithMessage("Uma ou mais permissões são inválidas")
                .Must((List<string>? list) => list!.Count <= 50)
                .WithMessage("Máximo de 50 permissões por papel");

            RuleForEach(x => x.Permissoes)
                .NotEmpty().WithMessage("Nome da permissão não pode ser vazio")
                .Matches(@"^[a-zA-Z0-9\.]+$").WithMessage("Permissão deve seguir o padrão: Recurso.Acao");
        });

        // ✅ VALIDAÇÃO DAS OBSERVAÇÕES (OPCIONAL)
        When(x => !string.IsNullOrEmpty(x.Observacoes), () =>
        {
            RuleFor(x => x.Observacoes)
                .MaximumLength(500).WithMessage("Observações devem ter no máximo 500 caracteres");
        });
    }

    /// <summary>
    /// Verifica se o nome do papel é único
    /// </summary>
    private async Task<bool> NomeDeveSerUnico(string nome, CancellationToken cancellationToken)
    {
        try
        {
            var papelExistente = await _roleManager.FindByNameAsync(nome);
            return papelExistente == null;
        }
        catch (Exception)
        {
            return false; // Em caso de erro, falha na validação
        }
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
        catch (Exception)
        {
            return false;
        }
    }
}