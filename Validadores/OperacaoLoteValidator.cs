using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Gestus.Modelos;
using Gestus.DTOs.Usuario;

namespace Gestus.Validadores;

/// <summary>
/// Validador FluentValidation para operações em lote
/// </summary>
public class OperacaoLoteValidator : AbstractValidator<SolicitacaoOperacaoLote>
{
    private readonly UserManager<Usuario> _userManager;
    private readonly RoleManager<Papel> _roleManager;

    public OperacaoLoteValidator(UserManager<Usuario> userManager, RoleManager<Papel> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;

        // ✅ VALIDAÇÃO DA OPERAÇÃO
        RuleFor(x => x.TipoOperacao)
            .NotEmpty().WithMessage("Tipo de operação é obrigatório")
            .Must(TipoOperacaoValido).WithMessage("Tipo de operação inválido");

        // ✅ VALIDAÇÕES ESPECÍFICAS POR OPERAÇÃO
        When(x => OperacaoRequerIds(x.TipoOperacao), () =>
        {
            RuleFor(x => x.UsuariosIds)
                .NotEmpty().WithMessage("Lista de IDs de usuários é obrigatória para esta operação")
                .Must(IdsNaoVazios).WithMessage("Lista de IDs não pode conter valores inválidos")
                .Must(IdsUnicos).WithMessage("Lista de IDs deve conter valores únicos");
        });

        When(x => OperacaoRequerDados(x.TipoOperacao), () =>
        {
            RuleFor(x => x.DadosUsuarios)
                .NotEmpty().WithMessage("Dados de usuários são obrigatórios para esta operação")
                .Must(DadosNaoVazios).WithMessage("Lista de dados não pode estar vazia");

            RuleForEach(x => x.DadosUsuarios)
                .SetValidator(new DadosUsuarioLoteValidator(_userManager))
                .When(x => x.DadosUsuarios != null);
        });

        // ✅ VALIDAÇÕES ESPECÍFICAS PARA ATRIBUIR/REMOVER PAPÉIS
        When(x => OperacaoRequerPapeis(x.TipoOperacao), () =>
        {
            RuleFor(x => x.ParametrosOperacao)
                .NotNull().WithMessage("Parâmetros de operação são obrigatórios para operações com papéis")
                .Must(ContemPapeis).WithMessage("Parâmetro 'papeis' deve ser fornecido para operações com papéis");

            RuleFor(x => x.ParametrosOperacao)
                .MustAsync(PapeisExistem).WithMessage("Alguns papéis especificados não existem ou estão inativos")
                .When(x => x.ParametrosOperacao != null && ContemPapeis(x.ParametrosOperacao));
        });

        // ✅ VALIDAÇÕES PARA EXPORTAÇÃO
        When(x => x.TipoOperacao.ToLower() == "exportar", () =>
        {
            RuleFor(x => x.ParametrosOperacao)
                .Must(ContemFormatoExportacao).WithMessage("Formato de exportação deve ser especificado (csv, xlsx, json)")
                .When(x => x.ParametrosOperacao != null);
        });

        // ✅ VALIDAÇÕES GERAIS
        RuleFor(x => x.UsuariosIds)
            .Must(x => x == null || x.Count <= 1000)
            .WithMessage("Máximo de 1000 usuários por operação em lote");

        RuleFor(x => x.DadosUsuarios)
            .Must(x => x == null || x.Count <= 500)
            .WithMessage("Máximo de 500 usuários para criação/atualização em lote");

        // ✅ VALIDAÇÃO COMBINADA
        RuleFor(x => x)
            .Must(TemDadosSuficientes)
            .WithMessage("Operação deve ter IDs de usuários ou dados de usuários, dependendo do tipo");
    }

    /// <summary>
    /// Verifica se o tipo de operação é válido
    /// </summary>
    private bool TipoOperacaoValido(string tipoOperacao)
    {
        if (string.IsNullOrWhiteSpace(tipoOperacao)) return false;

        var operacoesValidas = new[]
        {
            "ativar", "desativar", "excluir", 
            "atribuir-papeis", "remover-papeis", "limpar-papeis",
            "exportar", "criar", "atualizar",
            "reset-senha", "confirmar-email", "confirmar-telefone"
        };

        return operacoesValidas.Contains(tipoOperacao.ToLower());
    }

    /// <summary>
    /// Verifica se a operação requer IDs de usuários
    /// </summary>
    private bool OperacaoRequerIds(string tipoOperacao)
    {
        var operacoesComIds = new[]
        {
            "ativar", "desativar", "excluir",
            "atribuir-papeis", "remover-papeis", "limpar-papeis",
            "exportar", "reset-senha", "confirmar-email", "confirmar-telefone"
        };

        return operacoesComIds.Contains(tipoOperacao.ToLower());
    }

    /// <summary>
    /// Verifica se a operação requer dados de usuários
    /// </summary>
    private bool OperacaoRequerDados(string tipoOperacao)
    {
        var operacoesComDados = new[] { "criar", "atualizar" };
        return operacoesComDados.Contains(tipoOperacao.ToLower());
    }

    /// <summary>
    /// Verifica se a operação requer especificação de papéis
    /// </summary>
    private bool OperacaoRequerPapeis(string tipoOperacao)
    {
        var operacoesComPapeis = new[] { "atribuir-papeis", "remover-papeis" };
        return operacoesComPapeis.Contains(tipoOperacao.ToLower());
    }

    /// <summary>
    /// Verifica se a lista de IDs não está vazia
    /// </summary>
    private bool IdsNaoVazios(List<int>? ids)
    {
        return ids?.All(id => id > 0) ?? false;
    }

    /// <summary>
    /// Verifica se a lista de IDs contém valores únicos
    /// </summary>
    private bool IdsUnicos(List<int>? ids)
    {
        if (ids == null) return true;
        return ids.Count == ids.Distinct().Count();
    }

    /// <summary>
    /// Verifica se a lista de dados não está vazia
    /// </summary>
    private bool DadosNaoVazios(List<DadosUsuarioLote>? dados)
    {
        return dados?.Any() == true;
    }

    /// <summary>
    /// Verifica se os parâmetros contêm papéis
    /// </summary>
    private bool ContemPapeis(Dictionary<string, string>? parametros)
    {
        if (parametros == null) return false;
        
        return parametros.ContainsKey("papeis") && 
               !string.IsNullOrWhiteSpace(parametros["papeis"]);
    }

    /// <summary>
    /// Verifica se os papéis especificados existem e estão ativos
    /// </summary>
    private async Task<bool> PapeisExistem(Dictionary<string, string>? parametros, CancellationToken cancellationToken)
    {
        if (parametros == null || !parametros.ContainsKey("papeis")) return true;

        var papeisString = parametros["papeis"];
        if (string.IsNullOrWhiteSpace(papeisString)) return true;

        var papeis = papeisString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(p => p.Trim())
                                 .Where(p => !string.IsNullOrEmpty(p))
                                 .ToList();

        if (!papeis.Any()) return true;

        foreach (var nomePapel in papeis)
        {
            var papel = await _roleManager.FindByNameAsync(nomePapel);
            if (papel == null || !papel.Ativo)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Verifica se contém formato de exportação válido
    /// </summary>
    private bool ContemFormatoExportacao(Dictionary<string, string>? parametros)
    {
        if (parametros == null) return false;

        if (!parametros.ContainsKey("formato")) return false;

        var formato = parametros["formato"].ToLower();
        var formatosValidos = new[] { "csv", "xlsx", "json", "xml" };

        return formatosValidos.Contains(formato);
    }

    /// <summary>
    /// Verifica se a solicitação tem dados suficientes para a operação
    /// </summary>
    private bool TemDadosSuficientes(SolicitacaoOperacaoLote solicitacao)
    {
        return solicitacao.TipoOperacao.ToLower() switch
        {
            "criar" => solicitacao.DadosUsuarios?.Any() == true,
            "atualizar" => solicitacao.DadosUsuarios?.Any() == true,
            "exportar" when solicitacao.UsuariosIds?.Any() != true => 
                solicitacao.ParametrosOperacao?.ContainsKey("todos") == true,
            _ => solicitacao.UsuariosIds?.Any() == true || solicitacao.DadosUsuarios?.Any() == true
        };
    }
}

/// <summary>
/// Validador para dados individuais de usuário em lote
/// </summary>
public class DadosUsuarioLoteValidator : AbstractValidator<DadosUsuarioLote>
{
    private readonly UserManager<Usuario> _userManager;

    public DadosUsuarioLoteValidator(UserManager<Usuario> userManager)
    {
        _userManager = userManager;

        // ✅ VALIDAÇÕES CONDICIONAIS BASEADAS NO CONTEXTO
        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Email deve ter formato válido")
                .MaximumLength(256).WithMessage("Email deve ter no máximo 256 caracteres")
                .MustAsync(EmailUnicoParaNovoUsuario)
                .WithMessage("Email já existe para outro usuário")
                .When(x => x.Id == null); // Apenas para novos usuários
        });

        When(x => !string.IsNullOrEmpty(x.Nome), () =>
        {
            RuleFor(x => x.Nome)
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres")
                .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("Nome deve conter apenas letras e espaços");
        });

        When(x => !string.IsNullOrEmpty(x.Sobrenome), () =>
        {
            RuleFor(x => x.Sobrenome)
                .MaximumLength(150).WithMessage("Sobrenome deve ter no máximo 150 caracteres")
                .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("Sobrenome deve conter apenas letras e espaços");
        });

        When(x => !string.IsNullOrEmpty(x.Telefone), () =>
        {
            RuleFor(x => x.Telefone)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Telefone deve ter formato válido");
        });

        // ✅ SENHA OBRIGATÓRIA PARA CRIAÇÃO
        When(x => x.Id == null, () =>
        {
            RuleFor(x => x.Senha)
                .NotEmpty().WithMessage("Senha é obrigatória para criação de usuário")
                .MinimumLength(6).WithMessage("Senha deve ter pelo menos 6 caracteres")
                .Must(SenhaSegura).WithMessage("Senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número");
        });

        // ✅ SENHA OPCIONAL PARA ATUALIZAÇÃO
        When(x => x.Id.HasValue && !string.IsNullOrEmpty(x.Senha), () =>
        {
            RuleFor(x => x.Senha)
                .MinimumLength(6).WithMessage("Nova senha deve ter pelo menos 6 caracteres")
                .Must(SenhaSegura).WithMessage("Nova senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número");
        });

        RuleFor(x => x.Observacoes)
            .MaximumLength(500).WithMessage("Observações devem ter no máximo 500 caracteres");

        // ✅ VALIDAÇÃO DE PAPÉIS
        When(x => x.Papeis?.Any() == true, () =>
        {
            RuleForEach(x => x.Papeis)
                .NotEmpty().WithMessage("Nome do papel não pode estar vazio");
        });

        // ✅ PELO MENOS UM CAMPO DEVE SER FORNECIDO PARA ATUALIZAÇÃO
        When(x => x.Id.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(TemCampoParaAtualizar)
                .WithMessage("Pelo menos um campo deve ser fornecido para atualização");
        });

        // ✅ CAMPOS OBRIGATÓRIOS PARA CRIAÇÃO
        When(x => x.Id == null, () =>
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email é obrigatório para criação de usuário");

            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório para criação de usuário");

            RuleFor(x => x.Sobrenome)
                .NotEmpty().WithMessage("Sobrenome é obrigatório para criação de usuário");
        });
    }

    /// <summary>
    /// Verifica se email é único para novo usuário
    /// </summary>
    private async Task<bool> EmailUnicoParaNovoUsuario(DadosUsuarioLote dados, string email, CancellationToken cancellationToken)
    {
        if (dados.Id.HasValue) return true; // Para atualizações, não validar unicidade aqui
        
        var usuarioExistente = await _userManager.FindByEmailAsync(email);
        return usuarioExistente == null;
    }

    /// <summary>
    /// Verifica se a senha é segura
    /// </summary>
    private bool SenhaSegura(string senha)
    {
        if (string.IsNullOrEmpty(senha)) return false;

        return senha.Any(char.IsUpper) && 
               senha.Any(char.IsLower) && 
               senha.Any(char.IsDigit);
    }

    /// <summary>
    /// Verifica se há pelo menos um campo para atualizar
    /// </summary>
    private bool TemCampoParaAtualizar(DadosUsuarioLote dados)
    {
        return !string.IsNullOrEmpty(dados.Email) ||
               !string.IsNullOrEmpty(dados.Nome) ||
               !string.IsNullOrEmpty(dados.Sobrenome) ||
               !string.IsNullOrEmpty(dados.Telefone) ||
               !string.IsNullOrEmpty(dados.Senha) ||
               dados.Ativo.HasValue ||
               dados.EmailConfirmado.HasValue ||
               dados.TelefoneConfirmado.HasValue ||
               !string.IsNullOrEmpty(dados.Observacoes) ||
               dados.Papeis?.Any() == true;
    }
}