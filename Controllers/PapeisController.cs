using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Gestus.Modelos;
using Gestus.Dados;
using Gestus.DTOs.Papel;
using Gestus.DTOs.Comuns;
using Gestus.Validadores;
using System.Text.Json;
using OpenIddict.Abstractions;

namespace Gestus.Controllers;

/// <summary>
/// Controller para gerenciamento completo de papéis (roles)
/// Implementa operações CRUD robustas com paginação, filtros avançados e auditoria
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // ✅ SEMPRE autenticado para qualquer operação
public class PapeisController : ControllerBase
{
    private readonly UserManager<Usuario> _userManager;
    private readonly RoleManager<Papel> _roleManager;
    private readonly GestusDbContexto _context;
    private readonly ILogger<PapeisController> _logger;

    public PapeisController(
        UserManager<Usuario> userManager,
        RoleManager<Papel> roleManager,
        GestusDbContexto context,
        ILogger<PapeisController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista papéis com paginação, filtros e ordenação avançada
    /// </summary>
    /// <param name="filtros">Filtros de busca</param>
    /// <returns>Lista paginada de papéis</returns>
    [HttpGet]
    [ProducesResponseType(typeof(RespostaPaginada<PapelResumo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarPapeis([FromQuery] FiltrosPapel filtros)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();

            if (!TemPermissao("Papeis.Listar"))
            {
                return Forbid("Permissão insuficiente para listar papéis");
            }

            _logger.LogInformation("📋 Listando papéis - Filtros: {Filtros}, Usuário: {UsuarioLogadoId}", 
                filtros, usuarioLogadoId);

            // ✅ CONSTRUIR QUERY BASE
            var query = _context.Roles.AsQueryable();

            // ✅ APLICAR FILTRO BÁSICO DE ATIVOS (se não especificado incluir inativos)
            if (!filtros.IncluirInativos)
            {
                query = query.Where(r => r.Ativo);
            }

            // ✅ APLICAR FILTROS
            if (!string.IsNullOrEmpty(filtros.Nome))
            {
                query = query.Where(r => r.Name!.Contains(filtros.Nome));
            }

            if (!string.IsNullOrEmpty(filtros.Descricao))
            {
                query = query.Where(r => r.Descricao.Contains(filtros.Descricao));
            }

            if (!string.IsNullOrEmpty(filtros.Categoria))
            {
                query = query.Where(r => r.Categoria != null && r.Categoria.Contains(filtros.Categoria));
            }

            if (filtros.Ativo.HasValue)
            {
                query = query.Where(r => r.Ativo == filtros.Ativo.Value);
            }

            if (filtros.NivelMinimo.HasValue)
            {
                query = query.Where(r => r.Nivel >= filtros.NivelMinimo.Value);
            }

            if (filtros.NivelMaximo.HasValue)
            {
                query = query.Where(r => r.Nivel <= filtros.NivelMaximo.Value);
            }

            if (filtros.DataCriacaoInicio.HasValue)
            {
                query = query.Where(r => r.DataCriacao >= filtros.DataCriacaoInicio.Value);
            }

            if (filtros.DataCriacaoFim.HasValue)
            {
                query = query.Where(r => r.DataCriacao <= filtros.DataCriacaoFim.Value);
            }

            // ✅ FILTROS ESPECIAIS
            if (filtros.ApenasRolesSistema)
            {
                var rolesSistema = new[] { "SuperAdmin", "Admin", "Usuario" };
                query = query.Where(r => rolesSistema.Contains(r.Name!));
            }

            if (filtros.ApenasRolesPersonalizadas)
            {
                var rolesSistema = new[] { "SuperAdmin", "Admin", "Usuario" };
                query = query.Where(r => !rolesSistema.Contains(r.Name!));
            }

            if (filtros.Permissoes?.Any() == true)
            {
                query = query.Where(r => r.PapelPermissoes
                    .Where(pp => pp.Ativo)
                    .Any(pp => filtros.Permissoes.Contains(pp.Permissao.Nome)));
            }

            // ✅ CONTAR TOTAL ANTES DA PAGINAÇÃO
            var totalItens = await query.CountAsync();

            // ✅ APLICAR ORDENAÇÃO
            query = AplicarOrdenacao(query, filtros.OrdenarPor, filtros.DirecaoOrdenacao);

            // ✅ APLICAR PAGINAÇÃO
            var skip = (filtros.Pagina - 1) * filtros.ItensPorPagina;
            var papeis = await query
                .Skip(skip)
                .Take(filtros.ItensPorPagina)
                .Include(r => r.PapelPermissoes.Where(pp => pp.Ativo))
                    .ThenInclude(pp => pp.Permissao)
                .Include(r => r.UsuarioPapeis.Where(up => up.Ativo))
                .AsSplitQuery()
                .Select(r => new PapelResumo
                {
                    Id = r.Id,
                    Nome = r.Name!,
                    Descricao = r.Descricao,
                    Categoria = r.Categoria,
                    Nivel = r.Nivel,
                    Ativo = r.Ativo,
                    DataCriacao = r.DataCriacao,
                    DataAtualizacao = r.DataAtualizacao,
                    TotalPermissoes = r.PapelPermissoes.Count(pp => pp.Ativo),
                    TotalUsuarios = r.UsuarioPapeis.Count(up => up.Ativo),
                    PermissoesResumo = r.PapelPermissoes
                        .Where(pp => pp.Ativo)
                        .Take(5)
                        .Select(pp => pp.Permissao.Nome)
                        .ToList()
                })
                .ToListAsync();

            // ✅ CONSTRUIR RESPOSTA PAGINADA
            var totalPaginas = (int)Math.Ceiling((double)totalItens / filtros.ItensPorPagina);
            
            var resposta = new RespostaPaginada<PapelResumo>
            {
                Dados = papeis,
                PaginaAtual = filtros.Pagina,
                ItensPorPagina = filtros.ItensPorPagina,
                TotalItens = totalItens,
                TotalPaginas = totalPaginas,
                TemProximaPagina = filtros.Pagina < totalPaginas,
                TemPaginaAnterior = filtros.Pagina > 1
            };

            _logger.LogInformation("✅ Papéis listados - Total: {Total}, Página: {Pagina}/{TotalPaginas}", 
                totalItens, filtros.Pagina, totalPaginas);

            return Ok(resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar papéis");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao listar papéis"
            });
        }
    }

    /// <summary>
    /// Obtém detalhes completos de um papel específico
    /// </summary>
    /// <param name="id">ID do papel</param>
    /// <returns>Detalhes completos do papel</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PapelCompleto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterPapel(int id)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();

            if (!TemPermissao("Papeis.Visualizar"))
            {
                return Forbid("Permissão insuficiente para visualizar papéis");
            }

            _logger.LogInformation("👁️ Obtendo papel {Id} por usuário {UsuarioLogadoId}", id, usuarioLogadoId);

            var papel = await ObterPapelCompletoAsync(id);
            if (papel == null)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "PapelNaoEncontrado",
                    Mensagem = "Papel não encontrado"
                });
            }

            _logger.LogInformation("✅ Papel {Id} obtido com sucesso", id);
            return Ok(papel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter papel {Id}", id);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao obter papel"
            });
        }
    }

    /// <summary>
    /// Cria um novo papel no sistema
    /// </summary>
    /// <param name="request">Dados do novo papel</param>
    /// <returns>Papel criado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PapelCompleto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CriarPapel([FromBody] CriarPapelRequest request)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();

            if (!TemPermissao("Papeis.Criar"))
            {
                return Forbid("Permissão insuficiente para criar papéis");
            }

            _logger.LogInformation("➕ Criando papel {Nome} por usuário {UsuarioLogadoId}", 
                request.Nome, usuarioLogadoId);

            // ✅ VALIDAÇÃO FLUENT VALIDATION
            var validador = new CriarPapelValidator(_roleManager, HttpContext.RequestServices);
            var resultadoValidacao = await validador.ValidateAsync(request);
            
            if (!resultadoValidacao.IsValid)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "DadosInvalidos",
                    Mensagem = "Dados de criação são inválidos",
                    Detalhes = resultadoValidacao.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            // ✅ VERIFICAR SE JÁ EXISTE (double check)
            var papelExiste = await _roleManager.FindByNameAsync(request.Nome);
            if (papelExiste != null)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "PapelJaExiste",
                    Mensagem = "Já existe um papel com este nome"
                });
            }

            // ✅ CAPTURAR DADOS ANTES DA CRIAÇÃO
            var dadosAntes = new { };

            // ✅ CRIAR O PAPEL
            var novoPapel = new Papel
            {
                Name = request.Nome,
                NormalizedName = request.Nome.ToUpperInvariant(),
                Descricao = request.Descricao,
                Categoria = request.Categoria,
                Nivel = request.Nivel,
                Ativo = request.Ativo,
                DataCriacao = DateTime.UtcNow
            };

            var resultado = await _roleManager.CreateAsync(novoPapel);
            if (!resultado.Succeeded)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "ErroCriacao",
                    Mensagem = "Erro ao criar papel",
                    Detalhes = resultado.Errors.Select(e => e.Description).ToList()
                });
            }

            // ✅ ASSOCIAR PERMISSÕES SE FORNECIDAS
            if (request.Permissoes?.Any() == true)
            {
                await AssociarPermissoesAoPapel(novoPapel.Id, request.Permissoes, usuarioLogadoId);
            }

            // ✅ RECARREGAR PAPEL COM DADOS COMPLETOS
            var papelCriado = await ObterPapelCompletoAsync(novoPapel.Id);

            // ✅ CAPTURAR DADOS DEPOIS DA CRIAÇÃO
            var dadosDepois = new
            {
                papelCriado!.Id,
                papelCriado.Nome,
                papelCriado.Descricao,
                papelCriado.Categoria,
                papelCriado.Nivel,
                papelCriado.Ativo,
                TotalPermissoes = papelCriado.Permissoes.Count
            };

            // ✅ REGISTRAR AUDITORIA
            await RegistrarAuditoria("Papeis.Criar", "Papeis", novoPapel.Id.ToString(), 
                $"Papel criado: {request.Nome}", dadosAntes, dadosDepois);

            _logger.LogInformation("✅ Papel {Id} '{Nome}' criado com sucesso por {UsuarioLogadoId}", 
                novoPapel.Id, request.Nome, usuarioLogadoId);

            return CreatedAtAction(nameof(ObterPapel), new { id = novoPapel.Id }, papelCriado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao criar papel {Nome}", request.Nome);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao criar papel"
            });
        }
    }

    /// <summary>
    /// Atualiza um papel existente
    /// </summary>
    /// <param name="id">ID do papel a ser atualizado</param>
    /// <param name="request">Dados de atualização do papel</param>
    /// <returns>Papel atualizado</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PapelCompleto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AtualizarPapel(int id, [FromBody] AtualizarPapelRequest request)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();

            if (!TemPermissao("Papeis.Editar"))
            {
                return Forbid("Permissão insuficiente para editar papéis");
            }

            _logger.LogInformation("✏️ Atualizando papel {Id} por usuário {UsuarioLogadoId}", id, usuarioLogadoId);

            // ✅ VALIDAÇÃO FLUENT VALIDATION
            var validador = new AtualizarPapelValidator(_roleManager, HttpContext.RequestServices);
            var resultadoValidacao = await validador.ValidateAsync(request);
            
            if (!resultadoValidacao.IsValid)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "DadosInvalidos",
                    Mensagem = "Dados de atualização são inválidos",
                    Detalhes = resultadoValidacao.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            // ✅ BUSCAR PAPEL
            var papel = await _roleManager.FindByIdAsync(id.ToString());
            if (papel == null)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "PapelNaoEncontrado",
                    Mensagem = "Papel não encontrado"
                });
            }

            // ✅ VALIDAÇÃO MANUAL DE NOME ÚNICO (se fornecido)
            if (!string.IsNullOrEmpty(request.Nome) && request.Nome != papel.Name)
            {
                var papelComNome = await _roleManager.FindByNameAsync(request.Nome);
                if (papelComNome != null && papelComNome.Id != id)
                {
                    return BadRequest(new RespostaErro
                    {
                        Erro = "NomeJaExiste",
                        Mensagem = "Já existe outro papel com este nome"
                    });
                }
            }

            // ✅ VALIDAÇÕES DE SEGURANÇA
            if (papel.Name == "SuperAdmin" && request.Ativo == false)
            {
                // Verificar se há outros SuperAdmins ativos
                var totalSuperAdmins = await _context.Roles
                    .CountAsync(r => r.Name == "SuperAdmin" && r.Ativo);

                if (totalSuperAdmins <= 1)
                {
                    return BadRequest(new RespostaErro
                    {
                        Erro = "NaoPodeDesativarUltimoSuperAdmin",
                        Mensagem = "Não é possível desativar o último SuperAdmin do sistema"
                    });
                }
            }

            // ✅ CAPTURAR DADOS ANTES DA ALTERAÇÃO
            var dadosAntes = new
            {
                papel.Name,
                papel.Descricao,
                papel.Categoria,
                papel.Nivel,
                papel.Ativo
            };

            // ✅ APLICAR ALTERAÇÕES
            bool alterado = false;

            if (!string.IsNullOrEmpty(request.Nome) && request.Nome != papel.Name)
            {
                papel.Name = request.Nome;
                papel.NormalizedName = request.Nome.ToUpperInvariant();
                alterado = true;
            }

            if (!string.IsNullOrEmpty(request.Descricao) && request.Descricao != papel.Descricao)
            {
                papel.Descricao = request.Descricao;
                alterado = true;
            }

            if (!string.IsNullOrEmpty(request.Categoria) && request.Categoria != papel.Categoria)
            {
                papel.Categoria = request.Categoria;
                alterado = true;
            }

            if (request.Nivel.HasValue && request.Nivel != papel.Nivel)
            {
                papel.Nivel = request.Nivel.Value;
                alterado = true;
            }

            if (request.Ativo.HasValue && request.Ativo != papel.Ativo)
            {
                papel.Ativo = request.Ativo.Value;
                alterado = true;
            }

            // ✅ ATUALIZAR METADADOS SE HOUVE ALTERAÇÃO
            if (alterado)
            {
                papel.DataAtualizacao = DateTime.UtcNow;
            }

            // ✅ SALVAR ALTERAÇÕES DO PAPEL
            if (alterado)
            {
                var resultadoAtualizacao = await _roleManager.UpdateAsync(papel);
                if (!resultadoAtualizacao.Succeeded)
                {
                    return BadRequest(new RespostaErro
                    {
                        Erro = "ErroAtualizacao",
                        Mensagem = "Erro ao atualizar papel",
                        Detalhes = resultadoAtualizacao.Errors.Select(e => e.Description).ToList()
                    });
                }
            }

            // ✅ GERENCIAR PERMISSÕES SE ESPECIFICADAS
            if (request.Permissoes != null)
            {
                var permissoesAtuais = await ObterPermissoesDoPapel(papel.Id);
                var permissoesRemover = permissoesAtuais.Except(request.Permissoes).ToList();
                var permissoesAdicionar = request.Permissoes.Except(permissoesAtuais).ToList();

                // Remover permissões
                if (permissoesRemover.Any())
                {
                    await RemoverPermissoesDoPapel(papel.Id, permissoesRemover);
                }

                // Adicionar permissões
                if (permissoesAdicionar.Any())
                {
                    await AssociarPermissoesAoPapel(papel.Id, permissoesAdicionar, usuarioLogadoId);
                }

                if (permissoesRemover.Any() || permissoesAdicionar.Any())
                {
                    alterado = true;
                }
            }

            // ✅ RECARREGAR PAPEL COM DADOS ATUALIZADOS
            var papelAtualizado = await ObterPapelCompletoAsync(papel.Id);

            // ✅ CAPTURAR DADOS DEPOIS (para auditoria)
            var dadosDepois = new
            {
                papelAtualizado!.Nome,
                papelAtualizado.Descricao,
                papelAtualizado.Categoria,
                papelAtualizado.Nivel,
                papelAtualizado.Ativo,
                TotalPermissoes = papelAtualizado.Permissoes.Count
            };

            // ✅ REGISTRAR AUDITORIA SE HOUVE ALTERAÇÃO
            if (alterado)
            {
                await RegistrarAuditoria("Papeis.Atualizar", "Papeis", id.ToString(), 
                    $"Papel atualizado: {papelAtualizado.Nome}",
                    dadosAntes, dadosDepois);
            }

            _logger.LogInformation("✅ Papel {Id} atualizado com sucesso por {UsuarioLogadoId}", 
                id, usuarioLogadoId);

            return Ok(papelAtualizado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao atualizar papel {Id}", id);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao atualizar papel"
            });
        }
    }

    /// <summary>
    /// Remove um papel do sistema (soft delete por padrão)
    /// </summary>
    /// <param name="id">ID do papel a ser removido</param>
    /// <param name="exclusaoPermanente">Se true, faz hard delete (exclusão definitiva)</param>
    /// <returns>Confirmação da remoção</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(RespostaSucesso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoverPapel(int id, [FromQuery] bool exclusaoPermanente = false)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();

            if (!TemPermissao("Papeis.Excluir"))
            {
                return Forbid("Permissão insuficiente para excluir papéis");
            }

            _logger.LogInformation("🗑️ Removendo papel {Id} (permanente: {Permanente}) por usuário {UsuarioLogadoId}", 
                id, exclusaoPermanente, usuarioLogadoId);

            // ✅ BUSCAR PAPEL
            var papel = await _roleManager.FindByIdAsync(id.ToString());
            if (papel == null)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "PapelNaoEncontrado",
                    Mensagem = "Papel não encontrado"
                });
            }

            // ✅ VALIDAÇÕES DE SEGURANÇA
            if (papel.Name == "SuperAdmin")
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "NaoPodeExcluirSuperAdmin",
                    Mensagem = "Não é possível excluir o papel SuperAdmin"
                });
            }

            // ✅ CORRIGIDO: usar RoleId ao invés de PapelId
            var usuariosComPapel = await _context.Set<UsuarioPapel>()
                .CountAsync(ur => ur.RoleId == id);

            if (usuariosComPapel > 0 && exclusaoPermanente)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "PapelEmUso",
                    Mensagem = $"Não é possível excluir permanentemente. Há {usuariosComPapel} usuário(s) com este papel",
                    Detalhes = new List<string> { "Remova o papel de todos os usuários antes da exclusão permanente" }
                });
            }

            // ✅ CAPTURAR DADOS ANTES DA REMOÇÃO
            var dadosAntes = new
            {
                papel.Id,
                papel.Name,
                papel.Descricao,
                papel.Categoria,
                papel.Ativo,
                UsuariosComPapel = usuariosComPapel
            };

            string tipoOperacao;
            string mensagemSucesso;

            // ✅ EXECUTAR REMOÇÃO
            if (exclusaoPermanente && usuariosComPapel == 0)
            {
                // Hard delete - remoção definitiva
                var resultado = await _roleManager.DeleteAsync(papel);
                if (!resultado.Succeeded)
                {
                    return BadRequest(new RespostaErro
                    {
                        Erro = "ErroExclusao",
                        Mensagem = "Erro ao excluir papel permanentemente",
                        Detalhes = resultado.Errors.Select(e => e.Description).ToList()
                    });
                }

                tipoOperacao = "Papeis.ExcluirPermanente";
                mensagemSucesso = "Papel excluído permanentemente com sucesso";
            }
            else
            {
                // Soft delete - desativação
                papel.Ativo = false;
                papel.DataAtualizacao = DateTime.UtcNow;

                var resultado = await _roleManager.UpdateAsync(papel);
                if (!resultado.Succeeded)
                {
                    return BadRequest(new RespostaErro
                    {
                        Erro = "ErroDesativacao",
                        Mensagem = "Erro ao desativar papel",
                        Detalhes = resultado.Errors.Select(e => e.Description).ToList()
                    });
                }

                tipoOperacao = "Papeis.Desativar";
                mensagemSucesso = "Papel desativado com sucesso";
            }

            // ✅ REGISTRAR AUDITORIA
            await RegistrarAuditoria(tipoOperacao, "Papeis", id.ToString(), 
                $"Papel removido: {papel.Name} (permanente: {exclusaoPermanente})",
                dadosAntes, new { Removido = true, TipoRemocao = exclusaoPermanente ? "Permanente" : "Desativacao" });

            _logger.LogInformation("✅ Papel {Id} '{Nome}' removido com sucesso por {UsuarioLogadoId}", 
                id, papel.Name, usuarioLogadoId);

            return Ok(new RespostaSucesso
            {
                Sucesso = true,
                Mensagem = mensagemSucesso
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao remover papel {Id}", id);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao remover papel"
            });
        }
    }

    /// <summary>
    /// Reativa um papel desativado
    /// </summary>
    /// <param name="id">ID do papel a ser reativado</param>
    /// <returns>Confirmação da reativação</returns>
    [HttpPost("{id:int}/reativar")]
    [ProducesResponseType(typeof(PapelCompleto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReativarPapel(int id)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();

            if (!TemPermissao("Papeis.Editar"))
            {
                return Forbid("Permissão insuficiente para reativar papéis");
            }

            _logger.LogInformation("🔄 Reativando papel {Id} por usuário {UsuarioLogadoId}", id, usuarioLogadoId);

            // ✅ BUSCAR PAPEL (incluindo inativos)
            var papel = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id);
            if (papel == null)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "PapelNaoEncontrado",
                    Mensagem = "Papel não encontrado"
                });
            }

            if (papel.Ativo)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "PapelJaAtivo",
                    Mensagem = "O papel já está ativo"
                });
            }

            // ✅ REATIVAR PAPEL
            papel.Ativo = true;
            papel.DataAtualizacao = DateTime.UtcNow;

            var resultado = await _roleManager.UpdateAsync(papel);
            if (!resultado.Succeeded)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "ErroReativacao",
                    Mensagem = "Erro ao reativar papel",
                    Detalhes = resultado.Errors.Select(e => e.Description).ToList()
                });
            }

            // ✅ RECARREGAR PAPEL COM DADOS COMPLETOS
            var papelReativado = await ObterPapelCompletoAsync(papel.Id);

            // ✅ REGISTRAR AUDITORIA
            await RegistrarAuditoria("Papeis.Reativar", "Papeis", id.ToString(), 
                $"Papel reativado: {papel.Name}",
                new { Ativo = false }, new { Ativo = true });

            _logger.LogInformation("✅ Papel {Id} '{Nome}' reativado com sucesso por {UsuarioLogadoId}", 
                id, papel.Name, usuarioLogadoId);

            return Ok(papelReativado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao reativar papel {Id}", id);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao reativar papel"
            });
        }
    }

    /// <summary>
    /// Gerencia permissões de um papel específico
    /// </summary>
    /// <param name="id">ID do papel</param>
    /// <param name="request">Dados para gerenciamento de permissões</param>
    /// <returns>Permissões atualizadas do papel</returns>
    [HttpPost("{id:int}/permissoes")]
    [ProducesResponseType(typeof(RespostaGerenciamentoPermissoes), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GerenciarPermissoesPapel(int id, [FromBody] GerenciarPermissoesRequest request)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();

            if (!TemPermissao("Papeis.GerenciarPermissoes"))
            {
                return Forbid("Permissão insuficiente para gerenciar permissões de papéis");
            }

            _logger.LogInformation("🔧 Gerenciando permissões do papel {Id} - Operação: {Operacao} por usuário {UsuarioLogadoId}", 
                id, request.Operacao, usuarioLogadoId);

            // ✅ VALIDAÇÃO FLUENT VALIDATION
            var validador = new GerenciarPermissoesPapelValidator(HttpContext.RequestServices);
            var resultadoValidacao = await validador.ValidateAsync(request);
            
            if (!resultadoValidacao.IsValid)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "DadosInvalidos",
                    Mensagem = "Dados de gerenciamento de permissões são inválidos",
                    Detalhes = resultadoValidacao.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            // ✅ BUSCAR PAPEL
            var papel = await _roleManager.FindByIdAsync(id.ToString());
            if (papel == null)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "PapelNaoEncontrado",
                    Mensagem = "Papel não encontrado"
                });
            }

            if (!papel.Ativo)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "PapelInativo",
                    Mensagem = "Não é possível gerenciar permissões de um papel inativo"
                });
            }

            // ✅ EXECUTAR OPERAÇÃO
            var permissoesAtuais = await ObterPermissoesDoPapel(papel.Id);
            var resultado = await ExecutarOperacaoPermissoes(
                papel.Id, 
                request.Operacao, 
                request.Permissoes ?? new List<string>(), 
                permissoesAtuais, 
                usuarioLogadoId
            );

            // ✅ CORRIGIDO: Extrair propriedade para variável local (LINHA 859)
            var operacaoFoiSucesso = resultado.Sucesso;
                if (!operacaoFoiSucesso)
                {
                    return BadRequest(new RespostaErro
                    {
                        Erro = resultado.CodigoErro ?? "ErroOperacao",
                        Mensagem = resultado.Mensagem,
                        // ✅ CORREÇÃO: Filtrar valores nulos antes de converter para List<string>
                        Detalhes = resultado.Detalhes?.Values
                            .Select(v => v?.ToString())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Cast<string>()
                            .ToList()
                    });
                }

            // ✅ OBTER PERMISSÕES ATUALIZADAS
            var permissoesFinais = await ObterPermissoesDoPapel(papel.Id);
            var permissoesMapeadas = await _context.Permissoes
                .Where(p => permissoesFinais.Contains(p.Nome))
                .Select(p => new PermissaoPapel
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Descricao = p.Descricao,
                    Recurso = p.Recurso,
                    Acao = p.Acao,
                    Categoria = p.Categoria,
                    DataAtribuicao = DateTime.UtcNow
                })
                .ToListAsync();

            // ✅ CONSTRUIR RESPOSTA
            var resposta = new RespostaGerenciamentoPermissoes
            {
                Sucesso = true,
                Mensagem = resultado.Mensagem,
                Operacao = request.Operacao,
                PermissoesAfetadas = resultado.Detalhes?.ContainsKey("PermissoesAfetadas") == true ? 
                    (int)resultado.Detalhes["PermissoesAfetadas"] : 0,
                PermissoesAtuais = permissoesMapeadas,
                PermissoesAdicionadas = resultado.Detalhes?.ContainsKey("PermissoesAdicionadas") == true ?
                    (List<string>)resultado.Detalhes["PermissoesAdicionadas"] : new List<string>(),
                PermissoesRemovidas = resultado.Detalhes?.ContainsKey("PermissoesRemovidas") == true ?
                    (List<string>)resultado.Detalhes["PermissoesRemovidas"] : new List<string>(),
                Observacoes = request.Observacoes
            };

            // ✅ REGISTRAR AUDITORIA
            await RegistrarAuditoria("Papeis.GerenciarPermissoes", "Papeis", id.ToString(),
                $"Permissões do papel {papel.Name} - {request.Operacao}",
                new { PermissoesAnteriores = permissoesAtuais },
                new { PermissoesAtuais = permissoesFinais, Operacao = request.Operacao });

            _logger.LogInformation("✅ Permissões do papel {Id} gerenciadas com sucesso - {Operacao}", 
                id, request.Operacao);

            return Ok(resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao gerenciar permissões do papel {Id}", id);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao gerenciar permissões do papel"
            });
        }
    }

    /// <summary>
    /// Lista todas as permissões disponíveis no sistema
    /// </summary>
    /// <returns>Lista de permissões disponíveis</returns>
    [HttpGet("permissoes/disponiveis")]
    [ProducesResponseType(typeof(List<PermissaoDisponivel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarPermissoesDisponiveis()
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();

            if (!TemPermissao("Permissoes.Listar"))
            {
                return Forbid("Permissão insuficiente para listar permissões");
            }

            _logger.LogInformation("📋 Listando permissões disponíveis por usuário {UsuarioLogadoId}", usuarioLogadoId);

            var permissoes = await _context.Permissoes
                .Where(p => p.Ativo)
                .Select(p => new PermissaoDisponivel
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Descricao = p.Descricao,
                    Recurso = p.Recurso,
                    Acao = p.Acao,
                    Categoria = p.Categoria,
                    JaAtribuida = false, // Será calculado se necessário
                    Obrigatoria = p.Nome.StartsWith("Sistema."),
                    Critica = p.Nome.Contains("Excluir") || p.Nome.Contains("GerenciarPapeis") || p.Nome.StartsWith("Sistema."),
                    TotalPapeisComPermissao = p.PapelPermissoes.Count(pp => pp.Ativo)
                })
                .OrderBy(p => p.Categoria)
                .ThenBy(p => p.Recurso)
                .ThenBy(p => p.Acao)
                .ToListAsync();

            _logger.LogInformation("✅ {Total} permissões listadas", permissoes.Count);
            return Ok(permissoes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar permissões disponíveis");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao listar permissões disponíveis"
            });
        }
    }

    /// <summary>
    /// Lista usuários que possuem um papel específico
    /// </summary>
    /// <param name="id">ID do papel</param>
    /// <param name="filtros">Filtros de busca</param>
    /// <returns>Lista paginada de usuários com o papel</returns>
    [HttpGet("{id:int}/usuarios")]
    [ProducesResponseType(typeof(RespostaPaginada<UsuarioComPapel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarUsuariosDoPapel(int id, [FromQuery] FiltrosUsuariosPapel filtros)
    {
        try
        {
            // Verificar permissão
            if (!TemPermissao("Papeis.Visualizar"))
            {
                // ✅ CORRIGIDO: Usar StatusCode(403) ao invés de Forbid() com string
                return StatusCode(403, new RespostaErro
                {
                    Erro = "Acesso negado",
                    Mensagem = "Você não tem permissão para visualizar usuários de papéis"
                });
            }

            // Verificar se o papel existe
            var papel = await _roleManager.FindByIdAsync(id.ToString());
            if (papel == null)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "Papel não encontrado",
                    Mensagem = $"Papel com ID {id} não foi encontrado"
                });
            }

            // ✅ CORREÇÃO: Construir query base com Include ANTES do Where
            var query = _context.UsuarioPapeis
                .Include(up => up.Usuario)
                .Where(up => up.RoleId == id && up.Ativo);

            // Aplicar filtros
            if (!string.IsNullOrEmpty(filtros.Nome))
            {
                query = query.Where(up => up.Usuario.Nome.Contains(filtros.Nome) ||
                                        up.Usuario.Sobrenome.Contains(filtros.Nome));
            }

            if (!string.IsNullOrEmpty(filtros.Email))
            {
                query = query.Where(up => up.Usuario.Email!.Contains(filtros.Email));
            }

            if (filtros.Ativo.HasValue)
            {
                query = query.Where(up => up.Usuario.Ativo == filtros.Ativo);
            }

            if (filtros.DataAtribuicaoInicio.HasValue)
            {
                query = query.Where(up => up.DataAtribuicao >= filtros.DataAtribuicaoInicio);
            }

            if (filtros.DataAtribuicaoFim.HasValue)
            {
                query = query.Where(up => up.DataAtribuicao <= filtros.DataAtribuicaoFim);
            }

            if (!filtros.IncluirExpirados)
            {
                query = query.Where(up => !up.DataExpiracao.HasValue || up.DataExpiracao > DateTime.UtcNow);
            }

            // Contar total
            var total = await query.CountAsync();

            // Aplicar paginação
            var usuarios = await query
                .Skip((filtros.Pagina - 1) * filtros.ItensPorPagina)
                .Take(filtros.ItensPorPagina)
                .Select(up => new UsuarioComPapel
                {
                    Id = up.Usuario.Id,
                    Email = up.Usuario.Email!,
                    NomeCompleto = up.Usuario.NomeCompleto ?? $"{up.Usuario.Nome} {up.Usuario.Sobrenome}",
                    Ativo = up.Usuario.Ativo,
                    DataAtribuicao = up.DataAtribuicao,
                    DataExpiracao = up.DataExpiracao,
                    UltimoLogin = up.Usuario.UltimoLogin,
                    TotalPapeis = up.Usuario.UsuarioPapeis.Count(p => p.Ativo)
                })
                .ToListAsync();

            var resposta = new RespostaPaginada<UsuarioComPapel>
            {
                Dados = usuarios,
                TotalItens = total,
                PaginaAtual = filtros.Pagina,
                ItensPorPagina = filtros.ItensPorPagina,
                TotalPaginas = (int)Math.Ceiling((double)total / filtros.ItensPorPagina)
            };

            await RegistrarAuditoria("Listar", "PapelUsuarios", id.ToString(),
                $"Listados {usuarios.Count} usuários do papel {papel.Name}");

            return Ok(resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar usuários do papel {PapelId}", id);
            return StatusCode(500, new RespostaErro
            {
                Erro = "Erro interno",
                Mensagem = "Erro interno do servidor ao listar usuários do papel"
            });
        }
    }

    #region Métodos Auxiliares

    /// <summary>
    /// Aplica ordenação à query de papéis
    /// </summary>
    private IQueryable<Papel> AplicarOrdenacao(IQueryable<Papel> query, string? ordenarPor, string? direcao)
    {
        var crescente = string.IsNullOrEmpty(direcao) || direcao.ToLower() == "asc";
        
        return (ordenarPor?.ToLower()) switch
        {
            "nome" => crescente ? query.OrderBy(r => r.Name) : query.OrderByDescending(r => r.Name),
            "descricao" => crescente ? query.OrderBy(r => r.Descricao) : query.OrderByDescending(r => r.Descricao),
            "categoria" => crescente ? query.OrderBy(r => r.Categoria) : query.OrderByDescending(r => r.Categoria),
            "nivel" => crescente ? query.OrderBy(r => r.Nivel) : query.OrderByDescending(r => r.Nivel),
            "ativo" => crescente ? query.OrderBy(r => r.Ativo) : query.OrderByDescending(r => r.Ativo),
            "datacriacao" or _ => crescente ? query.OrderBy(r => r.DataCriacao) : query.OrderByDescending(r => r.DataCriacao)
        };
    }

    /// <summary>
    /// Obtém dados completos de um papel com relacionamentos
    /// </summary>
    private async Task<PapelCompleto?> ObterPapelCompletoAsync(int papelId)
    {
        var papel = await _context.Roles
            .Include(r => r.PapelPermissoes.Where(pp => pp.Ativo))
                .ThenInclude(pp => pp.Permissao)
            .Include(r => r.UsuarioPapeis.Where(up => up.Ativo))
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == papelId);

        if (papel == null)
        {
            return null;
        }

        // Calcular estatísticas - CORRIGIDO: usar RoleId
        var totalUsuarios = papel.UsuarioPapeis.Count(up => up.Ativo);
        var usuariosAtivos = await _context.Set<UsuarioPapel>()
            .Where(up => up.RoleId == papelId && up.Ativo)
            .Include(up => up.Usuario)
            .CountAsync(up => up.Usuario.Ativo);

        var totalUsuariosAtivos = await _context.Users.CountAsync(u => u.Ativo);
        
        var estatisticas = new EstatisticasPapel
        {
            TotalPermissoes = papel.PapelPermissoes.Count(pp => pp.Ativo),
            TotalUsuarios = totalUsuarios,
            UsuariosAtivos = usuariosAtivos,
            UsuariosInativos = totalUsuarios - usuariosAtivos,
            UltimaAtribuicao = await _context.Set<UsuarioPapel>()
                .Where(up => up.RoleId == papelId)
                .OrderByDescending(up => up.DataAtribuicao)
                .Select(up => up.DataAtribuicao)
                .FirstOrDefaultAsync(),
            PermissoesPorCategoria = papel.PapelPermissoes
                .Where(pp => pp.Ativo && !string.IsNullOrEmpty(pp.Permissao.Categoria))
                .GroupBy(pp => pp.Permissao.Categoria!)
                .ToDictionary(g => g.Key, g => g.Count()),
            PercentualUso = totalUsuariosAtivos > 0 ? (decimal)usuariosAtivos / totalUsuariosAtivos * 100 : 0
        };

        return new PapelCompleto
        {
            Id = papel.Id,
            Nome = papel.Name!,
            Descricao = papel.Descricao,
            Categoria = papel.Categoria,
            Nivel = papel.Nivel,
            Ativo = papel.Ativo,
            DataCriacao = papel.DataCriacao,
            DataAtualizacao = papel.DataAtualizacao,
            TotalUsuarios = totalUsuarios,
            Estatisticas = estatisticas,
            Permissoes = papel.PapelPermissoes
                .Where(pp => pp.Ativo)
                .Select(pp => new PermissaoPapel
                {
                    Id = pp.Permissao.Id,
                    Nome = pp.Permissao.Nome,
                    Descricao = pp.Permissao.Descricao,
                    Recurso = pp.Permissao.Recurso,
                    Acao = pp.Permissao.Acao,
                    Categoria = pp.Permissao.Categoria,
                    DataAtribuicao = pp.DataAtribuicao
                })
                .ToList()
        };
    }

    /// <summary>
    /// Obtém lista de permissões de um papel
    /// </summary>
    private async Task<List<string>> ObterPermissoesDoPapel(int papelId)
    {
        var permissoes = await _context.PapelPermissoes
            .Where(pp => pp.PapelId == papelId && pp.Ativo)
            .Include(pp => pp.Permissao)
            .Select(pp => pp.Permissao.Nome)
            .Where(nome => !string.IsNullOrEmpty(nome)) // Filtrar nulos
            .ToListAsync();

        return permissoes!; // Cast seguro após filtrar nulos
    }

    /// <summary>
    /// Associa permissões a um papel
    /// </summary>
    private async Task AssociarPermissoesAoPapel(int papelId, List<string> nomesPermissoes, int atribuidoPorId)
    {
        var permissoes = await _context.Permissoes
            .Where(p => nomesPermissoes.Contains(p.Nome) && p.Ativo)
            .ToListAsync();

        var novasAssociacoes = permissoes.Select(p => new PapelPermissao
        {
            PapelId = papelId,
            PermissaoId = p.Id,
            DataAtribuicao = DateTime.UtcNow,
            Ativo = true
        }).ToList();

        _context.Set<PapelPermissao>().AddRange(novasAssociacoes);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Remove permissões de um papel
    /// </summary>
    private async Task RemoverPermissoesDoPapel(int papelId, List<string> nomesPermissoes)
    {
        var associacoes = await _context.Set<PapelPermissao>()
            .Where(pp => pp.PapelId == papelId && pp.Ativo)
            .Include(pp => pp.Permissao)
            .Where(pp => nomesPermissoes.Contains(pp.Permissao.Nome))
            .ToListAsync();

        foreach (var associacao in associacoes)
        {
            associacao.Ativo = false;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Executa operação de gerenciamento de permissões
    /// </summary>
    private async Task<ResultadoOperacao> ExecutarOperacaoPermissoes(int papelId, string operacao, 
        List<string> permissoes, List<string> permissoesAtuais, int usuarioLogadoId)
    {
        try
        {
            switch (operacao.ToLower())
            {
                case "substituir":
                    // Remove todas as permissões atuais
                    await RemoverPermissoesDoPapel(papelId, permissoesAtuais);
                    
                    // Adiciona as novas permissões
                    if (permissoes.Any())
                    {
                        await AssociarPermissoesAoPapel(papelId, permissoes, usuarioLogadoId);
                    }
                    
                    // ✅ CORRIGIDO: Acessar propriedade, não método
                    return ResultadoOperacao.CriarSucesso($"Permissões substituídas com sucesso. {permissoes.Count} permissões definidas.");

                case "adicionar":
                    // Adiciona apenas permissões que ainda não existem
                    var permissoesParaAdicionar = permissoes.Except(permissoesAtuais).ToList();
                    
                    if (permissoesParaAdicionar.Any())
                    {
                        await AssociarPermissoesAoPapel(papelId, permissoesParaAdicionar, usuarioLogadoId);
                    }
                    
                    // ✅ CORRIGIDO: Acessar propriedade, não método  
                    return ResultadoOperacao.CriarSucesso($"{permissoesParaAdicionar.Count} permissões adicionadas com sucesso.");

                case "remover":
                    // Remove apenas permissões que existem
                    var permissoesParaRemover = permissoes.Intersect(permissoesAtuais).ToList();
                    
                    if (permissoesParaRemover.Any())
                    {
                        await RemoverPermissoesDoPapel(papelId, permissoesParaRemover);
                    }
                    
                    // ✅ CORRIGIDO: Acessar propriedade, não método
                    return ResultadoOperacao.CriarSucesso($"{permissoesParaRemover.Count} permissões removidas com sucesso.");

                case "limpar":
                    // Remove todas as permissões
                    await RemoverPermissoesDoPapel(papelId, permissoesAtuais);
                    
                    // ✅ CORRIGIDO: Acessar propriedade, não método
                    return ResultadoOperacao.CriarSucesso($"Todas as {permissoesAtuais.Count} permissões foram removidas.");

                default:
                    return ResultadoOperacao.CriarErro("Operação inválida", "OPERACAO_INVALIDA");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar operação de permissões {Operacao} para papel {PapelId}", operacao, papelId);
            return ResultadoOperacao.CriarErro($"Erro interno ao executar operação: {ex.Message}", "ERRO_INTERNO");
        }
    }

    /// <summary>
    /// Obtém ID do usuário logado a partir do token
    /// </summary>
    private int ObterUsuarioLogadoId()
    {
        // ✅ CORREÇÃO: Especificar explicitamente o tipo
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                            User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(usuarioIdClaim) || !int.TryParse(usuarioIdClaim, out var usuarioId))
        {
            throw new UnauthorizedAccessException("Usuário não autenticado ou ID inválido");
        }

        return usuarioId;
    }

    /// <summary>
    /// Verifica se o usuário logado tem uma permissão específica
    /// </summary>
    private bool TemPermissao(string permissao)
    {
        try
        {
            // ✅ CORREÇÃO: Especificar explicitamente o tipo do delegate
            var permissoesClaim = User.Claims
                .Where(c => c.Type == "permissions")
                .Select(c => c.Value)
                .ToList();

            return permissoesClaim.Contains(permissao) || 
                   User.IsInRole("SuperAdmin");
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Registra uma ação na auditoria
    /// </summary>
    private async Task RegistrarAuditoria(string acao, string recurso, string? recursoId, 
        string observacoes, object? dadosAntes = null, object? dadosDepois = null)
    {
        try
        {
            var usuarioId = ObterUsuarioLogadoId();
            
            var registro = new RegistroAuditoria
            {
                UsuarioId = usuarioId,
                Acao = acao,
                Recurso = recurso,
                RecursoId = recursoId,
                Observacoes = observacoes,
                DadosAntes = dadosAntes != null ? JsonSerializer.Serialize(dadosAntes) : null,
                DadosDepois = dadosDepois != null ? JsonSerializer.Serialize(dadosDepois) : null,
                DataHora = DateTime.UtcNow,
                EnderecoIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconhecido",
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
            };

            _context.RegistrosAuditoria.Add(registro);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar auditoria para ação {Acao}", acao);
        }
    }

    #endregion
}