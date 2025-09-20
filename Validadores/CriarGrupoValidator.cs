using FluentValidation;
using Gestus.DTOs.Grupo;
using Gestus.Dados;
using Microsoft.EntityFrameworkCore;

namespace Gestus.Validadores;

public class CriarGrupoValidator : AbstractValidator<CriarGrupoRequest>
{
    private readonly GestusDbContexto _context;

    public CriarGrupoValidator(GestusDbContexto context)
    {
        _context = context;

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .Length(3, 100).WithMessage("Nome deve ter entre 3 e 100 caracteres")
            .MustAsync(NomeUnico).WithMessage("Já existe um grupo com este nome");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição é obrigatória")
            .Length(10, 200).WithMessage("Descrição deve ter entre 10 e 200 caracteres");

        RuleFor(x => x.Tipo)
            .MaximumLength(50).WithMessage("Tipo deve ter no máximo 50 caracteres");

        When(x => x.UsuariosIds?.Any() == true, () =>
        {
            RuleFor(x => x.UsuariosIds!)
                .Must(ids => ids.Count <= 100).WithMessage("Máximo de 100 usuários por operação");

            RuleForEach(x => x.UsuariosIds!)
                .MustAsync(UsuarioExiste).WithMessage("Usuário com ID {PropertyValue} não existe");
        });
    }

    private async Task<bool> NomeUnico(string nome, CancellationToken cancellationToken)
    {
        return !await _context.Grupos
            .AnyAsync(g => g.Nome.ToLower() == nome.ToLower(), cancellationToken);
    }

    private async Task<bool> UsuarioExiste(int usuarioId, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AnyAsync(u => u.Id == usuarioId && u.Ativo, cancellationToken);
    }
}