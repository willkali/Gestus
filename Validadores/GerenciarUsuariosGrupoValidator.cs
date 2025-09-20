using FluentValidation;
using Gestus.DTOs.Grupo;
using Gestus.Dados;
using Microsoft.EntityFrameworkCore;

namespace Gestus.Validadores;

public class GerenciarUsuariosGrupoValidator : AbstractValidator<GerenciarUsuariosGrupoRequest>
{
    private readonly GestusDbContexto _context;

    public GerenciarUsuariosGrupoValidator(GestusDbContexto context)
    {
        _context = context;

        RuleFor(x => x.Operacao)
            .NotEmpty().WithMessage("Operação é obrigatória")
            .Must(operacao => new[] { "adicionar", "remover", "substituir", "limpar" }
                .Contains(operacao.ToLower()))
            .WithMessage("Operação deve ser: adicionar, remover, substituir ou limpar");

        When(x => x.Operacao.ToLower() != "limpar", () =>
        {
            RuleFor(x => x.UsuariosIds)
                .NotEmpty().WithMessage("Lista de usuários é obrigatória para esta operação")
                .Must(ids => ids.Count <= 100)
                .WithMessage("Máximo de 100 usuários por operação");

            RuleForEach(x => x.UsuariosIds)
                .GreaterThan(0).WithMessage("ID do usuário deve ser maior que zero")
                .MustAsync(UsuarioExiste).WithMessage("Usuário com ID {PropertyValue} não existe");
        });
    }

    private async Task<bool> UsuarioExiste(int usuarioId, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AnyAsync(u => u.Id == usuarioId && u.Ativo, cancellationToken);
    }
}