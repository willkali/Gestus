using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Gestus.Modelos;
using Gestus.Dados;
using Gestus.DTOs.Permissao;
using Gestus.DTOs.Comuns;
using System.Text.Json;

namespace Gestus.Controllers;

/// <summary>
/// Controller para gerenciamento completo de permissões
/// Implementa operações CRUD robustas com paginação, filtros avançados e auditoria
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // ✅ SEMPRE autenticado para qualquer operação
public class PermissoesController : ControllerBase
{
    private readonly GestusDbContexto _context;
    private readonly ILogger<PermissoesController> _logger;

    public PermissoesController(
        GestusDbContexto context,
        ILogger<PermissoesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista permissões com paginação, filtros e ordenação avançada
    /// </summary>
    /// <param name="filtros">Filtros de busca</param>
    /// <returns>Lista paginada de permissões</returns>
    [HttpGet]
    [ProducesResponseType(typeof(RespostaPaginada<PermissaoResumo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarPermissoes([FromQuery] FiltrosPermissao filtros)
    {
        try
        {
            _logger.LogInformation("📋 Listando permissões - Usuário: {UsuarioId}", ObterUsuarioLogadoId());

            // ✅ VERIFICAR PERMISSÃO
            if (!TemPermissao("Permissoes.Listar"))
            {
                return Forbid("Usuário não tem permissão para listar permissões");
            }

            // ✅ VALIDAR FILTROS
            if (filtros.Pagina <= 0) filtros.Pagina = 1;
            if (filtros.ItensPorPagina <= 0 || filtros.ItensPorPagina > 100) filtros.ItensPorPagina = 20;

            // ✅ CONSTRUIR QUERY BASE
            var query = _context.Permissoes.AsQueryable();

            // ✅ APLICAR FILTROS
            if (!filtros.IncluirInativas)
            {
                query = query.Where(p => p.Ativo);
            }

            if (!string.IsNullOrEmpty(filtros.Nome))
            {
                query = query.Where(p => p.Nome.ToLower().Contains(filtros.Nome.ToLower()));
            }

            if (!string.IsNullOrEmpty(filtros.Descricao))
            {
                query = query.Where(p => p.Descricao.ToLower().Contains(filtros.Descricao.ToLower()));
            }

            if (!string.IsNullOrEmpty(filtros.Recurso))
            {
                query = query.Where(p => p.Recurso.ToLower().Contains(filtros.Recurso.ToLower()));
            }

            if (!string.IsNullOrEmpty(filtros.Acao))
            {
                query = query.Where(p => p.Acao.ToLower().Contains(filtros.Acao.ToLower()));
            }

            if (!string.IsNullOrEmpty(filtros.Categoria))
            {
                query = query.Where(p => p.Categoria != null && p.Categoria.ToLower().Contains(filtros.Categoria.ToLower()));
            }

            if (filtros.Ativo.HasValue)
            {
                query = query.Where(p => p.Ativo == filtros.Ativo.Value);
            }

            if (filtros.DataCriacaoInicio.HasValue)
            {
                query = query.Where(p => p.DataCriacao >= filtros.DataCriacaoInicio.Value);
            }

            if (filtros.DataCriacaoFim.HasValue)
            {
                query = query.Where(p => p.DataCriacao <= filtros.DataCriacaoFim.Value);
            }

            // ✅ APLICAR ORDENAÇÃO
            query = AplicarOrdenacao(query, filtros.OrdenarPor, filtros.DirecaoOrdenacao);

            // ✅ CONTAR TOTAL
            var totalItens = await query.CountAsync();

            // ✅ APLICAR PAGINAÇÃO
            var permissoes = await query
                .Skip((filtros.Pagina - 1) * filtros.ItensPorPagina)
                .Take(filtros.ItensPorPagina)
                .Select(p => new PermissaoResumo
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Descricao = p.Descricao,
                    Recurso = p.Recurso,
                    Acao = p.Acao,
                    Categoria = p.Categoria,
                    Ativo = p.Ativo,
                    DataCriacao = p.DataCriacao,
                    TotalPapeis = p.PapelPermissoes.Count(pp => pp.Ativo)
                })
                .ToListAsync();

            // ✅ MONTAR RESPOSTA PAGINADA
            var resposta = new RespostaPaginada<PermissaoResumo>
            {
                Dados = permissoes,
                PaginaAtual = filtros.Pagina,
                ItensPorPagina = filtros.ItensPorPagina,
                TotalItens = totalItens,
                TotalPaginas = (int)Math.Ceiling((double)totalItens / filtros.ItensPorPagina),
                TemProximaPagina = filtros.Pagina < (int)Math.Ceiling((double)totalItens / filtros.ItensPorPagina),
                TemPaginaAnterior = filtros.Pagina > 1
            };

            _logger.LogInformation("✅ Listagem concluída - {Total} permissões encontradas", totalItens);
            return Ok(resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar permissões");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno do servidor ao listar permissões"
            });
        }
    }

    /// <summary>
    /// Obtém detalhes completos de uma permissão específica
    /// </summary>
    /// <param name="id">ID da permissão</param>
    /// <returns>Detalhes completos da permissão</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PermissaoCompleta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterPermissao(int id)
    {
        try
        {
            _logger.LogInformation("🔍 Obtendo permissão {Id} - Usuário: {UsuarioId}", id, ObterUsuarioLogadoId());

            // ✅ VERIFICAR PERMISSÃO
            if (!TemPermissao("Permissoes.Visualizar"))
            {
                return Forbid("Usuário não tem permissão para visualizar permissões");
            }

            // ✅ BUSCAR PERMISSÃO COM RELACIONAMENTOS
            var permissaoCompleta = await ObterPermissaoCompletaAsync(id);

            if (permissaoCompleta == null)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "PermissaoNaoEncontrada",
                    Mensagem = $"Permissão com ID {id} não foi encontrada"
                });
            }

            _logger.LogInformation("✅ Permissão {Id} encontrada: {Nome}", id, permissaoCompleta.Nome);
            return Ok(permissaoCompleta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter permissão {Id}", id);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno do servidor ao obter permissão"
            });
        }
    }

    /// <summary>
    /// Cria uma nova permissão no sistema
    /// </summary>
    /// <param name="request">Dados da nova permissão</param>
    /// <returns>Permissão criada</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PermissaoCompleta), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CriarPermissao([FromBody] CriarPermissaoRequest request)
    {
        try
        {
            _logger.LogInformation("🆕 Criando permissão '{Nome}' - Usuário: {UsuarioId}", request.Nome, ObterUsuarioLogadoId());

            // ✅ VERIFICAR PERMISSÃO
            if (!TemPermissao("Permissoes.Criar"))
            {
                return Forbid("Usuário não tem permissão para criar permissões");
            }

            // ✅ VALIDAR SE PERMISSÃO JÁ EXISTE
            var permissaoExistente = await _context.Permissoes
                .FirstOrDefaultAsync(p => p.Nome == request.Nome || 
                                         (p.Recurso == request.Recurso && p.Acao == request.Acao));

            if (permissaoExistente != null)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "PermissaoJaExiste",
                    Mensagem = permissaoExistente.Nome == request.Nome 
                        ? $"Já existe uma permissão com o nome '{request.Nome}'"
                        : $"Já existe uma permissão para o recurso '{request.Recurso}' com a ação '{request.Acao}'"
                });
            }

            // ✅ CRIAR NOVA PERMISSÃO
            var novaPermissao = new Permissao
            {
                Nome = request.Nome,
                Descricao = request.Descricao,
                Recurso = request.Recurso,
                Acao = request.Acao,
                Categoria = request.Categoria,
                Ativo = true,
                DataCriacao = DateTime.UtcNow
            };

            _context.Permissoes.Add(novaPermissao);
            await _context.SaveChangesAsync();

            // ✅ REGISTRAR AUDITORIA
            await RegistrarAuditoria("Criar", "Permissao", novaPermissao.Id.ToString(),
                $"Permissão '{novaPermissao.Nome}' criada", null, novaPermissao);

            // ✅ BUSCAR DADOS COMPLETOS PARA RESPOSTA
            var permissaoCompleta = await ObterPermissaoCompletaAsync(novaPermissao.Id);

            _logger.LogInformation("✅ Permissão criada com sucesso - ID: {Id}, Nome: {Nome}", 
                novaPermissao.Id, novaPermissao.Nome);

            return CreatedAtAction(nameof(ObterPermissao), new { id = novaPermissao.Id }, permissaoCompleta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao criar permissão '{Nome}'", request.Nome);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno do servidor ao criar permissão"
            });
        }
    }
    
    /// <summary>
    /// Atualiza uma permissão existente
    /// </summary>
    /// <param name="id">ID da permissão a ser atualizada</param>
    /// <param name="request">Dados de atualização da permissão</param>
    /// <returns>Permissão atualizada</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PermissaoCompleta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AtualizarPermissao(int id, [FromBody] AtualizarPermissaoRequest request)
    {
        try
        {
            _logger.LogInformation("🔄 Atualizando permissão {Id} - Usuário: {UsuarioId}", id, ObterUsuarioLogadoId());

            // ✅ VERIFICAR PERMISSÃO
            if (!TemPermissao("Permissoes.Editar"))
            {
                return Forbid("Usuário não tem permissão para editar permissões");
            }

            // ✅ BUSCAR PERMISSÃO EXISTENTE
            var permissao = await _context.Permissoes.FindAsync(id);
            if (permissao == null)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "PermissaoNaoEncontrada",
                    Mensagem = $"Permissão com ID {id} não foi encontrada"
                });
            }

            // ✅ VALIDAR CONFLITOS DE DUPLICAÇÃO
            if (!string.IsNullOrEmpty(request.Nome) && request.Nome != permissao.Nome)
            {
                var permissaoComNome = await _context.Permissoes
                    .FirstOrDefaultAsync(p => p.Nome == request.Nome && p.Id != id);

                if (permissaoComNome != null)
                {
                    return BadRequest(new RespostaErro
                    {
                        Erro = "NomeJaExiste",
                        Mensagem = $"Já existe uma permissão com o nome '{request.Nome}'"
                    });
                }
            }

            if (!string.IsNullOrEmpty(request.Recurso) && !string.IsNullOrEmpty(request.Acao) &&
                (request.Recurso != permissao.Recurso || request.Acao != permissao.Acao))
            {
                var permissaoComRecursoAcao = await _context.Permissoes
                    .FirstOrDefaultAsync(p => p.Recurso == request.Recurso && p.Acao == request.Acao && p.Id != id);

                if (permissaoComRecursoAcao != null)
                {
                    return BadRequest(new RespostaErro
                    {
                        Erro = "RecursoAcaoJaExiste",
                        Mensagem = $"Já existe uma permissão para o recurso '{request.Recurso}' com a ação '{request.Acao}'"
                    });
                }
            }

            // ✅ GUARDAR DADOS ORIGINAIS PARA AUDITORIA
            var dadosAntes = new
            {
                permissao.Nome,
                permissao.Descricao,
                permissao.Recurso,
                permissao.Acao,
                permissao.Categoria,
                permissao.Ativo
            };

            // ✅ APLICAR ALTERAÇÕES
            var alterado = false;

            if (!string.IsNullOrEmpty(request.Nome) && request.Nome != permissao.Nome)
            {
                permissao.Nome = request.Nome;
                alterado = true;
            }

            if (!string.IsNullOrEmpty(request.Descricao) && request.Descricao != permissao.Descricao)
            {
                permissao.Descricao = request.Descricao;
                alterado = true;
            }

            if (!string.IsNullOrEmpty(request.Recurso) && request.Recurso != permissao.Recurso)
            {
                permissao.Recurso = request.Recurso;
                alterado = true;
            }

            if (!string.IsNullOrEmpty(request.Acao) && request.Acao != permissao.Acao)
            {
                permissao.Acao = request.Acao;
                alterado = true;
            }

            if (!string.IsNullOrEmpty(request.Categoria) && request.Categoria != permissao.Categoria)
            {
                permissao.Categoria = request.Categoria;
                alterado = true;
            }

            if (request.Ativo.HasValue && request.Ativo != permissao.Ativo)
            {
                permissao.Ativo = request.Ativo.Value;
                alterado = true;
            }

            // ✅ SALVAR APENAS SE HOUVE ALTERAÇÕES
            if (alterado)
            {
                await _context.SaveChangesAsync();

                // ✅ REGISTRAR AUDITORIA
                var dadosDepois = new
                {
                    permissao.Nome,
                    permissao.Descricao,
                    permissao.Recurso,
                    permissao.Acao,
                    permissao.Categoria,
                    permissao.Ativo
                };

                await RegistrarAuditoria("Atualizar", "Permissao", permissao.Id.ToString(),
                    $"Permissão '{permissao.Nome}' atualizada", dadosAntes, dadosDepois);

                _logger.LogInformation("✅ Permissão {Id} atualizada com sucesso", id);
            }
            else
            {
                _logger.LogInformation("ℹ️ Nenhuma alteração detectada para permissão {Id}", id);
            }

            // ✅ RETORNAR DADOS ATUALIZADOS
            var permissaoCompleta = await ObterPermissaoCompletaAsync(id);
            return Ok(permissaoCompleta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao atualizar permissão {Id}", id);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno do servidor ao atualizar permissão"
            });
        }
    }

    /// <summary>
    /// Remove uma permissão do sistema (soft delete por padrão)
    /// </summary>
    /// <param name="id">ID da permissão a ser removida</param>
    /// <param name="exclusaoPermanente">Se true, faz hard delete (exclusão definitiva)</param>
    /// <returns>Confirmação da remoção</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(RespostaSucesso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoverPermissao(int id, [FromQuery] bool exclusaoPermanente = false)
    {
        try
        {
            _logger.LogInformation("🗑️ Removendo permissão {Id} (permanente: {Permanente}) - Usuário: {UsuarioId}", 
                id, exclusaoPermanente, ObterUsuarioLogadoId());

            // ✅ VERIFICAR PERMISSÃO
            if (!TemPermissao("Permissoes.Excluir"))
            {
                return Forbid("Usuário não tem permissão para excluir permissões");
            }

            // ✅ BUSCAR PERMISSÃO
            var permissao = await _context.Permissoes
                .Include(p => p.PapelPermissoes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permissao == null)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "PermissaoNaoEncontrada",
                    Mensagem = $"Permissão com ID {id} não foi encontrada"
                });
            }

            // ✅ VERIFICAR SE A PERMISSÃO ESTÁ EM USO
            var papeisComPermissao = await _context.PapelPermissoes
                .Where(pp => pp.PermissaoId == id && pp.Ativo)
                .CountAsync();

            if (papeisComPermissao > 0 && exclusaoPermanente)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "PermissaoEmUso",
                    Mensagem = $"Não é possível excluir permanentemente a permissão. Ela está associada a {papeisComPermissao} papel(éis). Remova as associações primeiro ou use exclusão lógica."
                });
            }

            // ✅ VERIFICAR PERMISSÕES DO SISTEMA (NÃO PODEM SER EXCLUÍDAS)
            var permissoesSistema = new[]
            {
                "Sistema.Controle.Total",
                "Sistema.Configuracao.Gerenciar",
                "Usuarios.Criar", "Usuarios.Listar", "Usuarios.Visualizar", "Usuarios.Editar", "Usuarios.Excluir",
                "Papeis.Criar", "Papeis.Listar", "Papeis.Visualizar", "Papeis.Editar", "Papeis.Excluir",
                "Permissoes.Criar", "Permissoes.Listar", "Permissoes.Visualizar", "Permissoes.Editar", "Permissoes.Excluir"
            };

            if (permissoesSistema.Contains(permissao.Nome))
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "PermissaoSistema",
                    Mensagem = $"A permissão '{permissao.Nome}' é uma permissão do sistema e não pode ser excluída."
                });
            }

            // ✅ GUARDAR DADOS PARA AUDITORIA
            var dadosAntes = new
            {
                permissao.Id,
                permissao.Nome,
                permissao.Descricao,
                permissao.Recurso,
                permissao.Acao,
                permissao.Categoria,
                permissao.Ativo,
                TotalPapeis = papeisComPermissao
            };

            // ✅ EXECUTAR REMOÇÃO
            if (exclusaoPermanente && papeisComPermissao == 0)
            {
                // ✅ HARD DELETE - Remove definitivamente
                _context.Permissoes.Remove(permissao);
                await _context.SaveChangesAsync();

                await RegistrarAuditoria("ExcluirPermanente", "Permissao", id.ToString(),
                    $"Permissão '{permissao.Nome}' excluída permanentemente", dadosAntes, null);

                _logger.LogInformation("✅ Permissão {Id} excluída permanentemente", id);

                return Ok(new RespostaSucesso
                {
                    Sucesso = true,
                    Mensagem = $"Permissão '{permissao.Nome}' foi excluída permanentemente do sistema."
                });
            }
            else
            {
                // ✅ SOFT DELETE - Apenas desativa
                if (permissao.Ativo)
                {
                    permissao.Ativo = false;
                    await _context.SaveChangesAsync();

                    await RegistrarAuditoria("Desativar", "Permissao", id.ToString(),
                        $"Permissão '{permissao.Nome}' desativada", dadosAntes, new { permissao.Ativo });

                    _logger.LogInformation("✅ Permissão {Id} desativada", id);

                    return Ok(new RespostaSucesso
                    {
                        Sucesso = true,
                        Mensagem = $"Permissão '{permissao.Nome}' foi desativada. As associações com papéis foram mantidas."
                    });
                }
                else
                {
                    return BadRequest(new RespostaErro
                    {
                        Erro = "PermissaoJaInativa",
                        Mensagem = "A permissão já está inativa"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao remover permissão {Id}", id);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno do servidor ao remover permissão"
            });
        }
    }

    /// <summary>
    /// Reativa uma permissão desativada
    /// </summary>
    /// <param name="id">ID da permissão a ser reativada</param>
    /// <returns>Confirmação da reativação</returns>
    [HttpPost("{id:int}/reativar")]
    [ProducesResponseType(typeof(PermissaoCompleta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReativarPermissao(int id)
    {
        try
        {
            _logger.LogInformation("🔄 Reativando permissão {Id} - Usuário: {UsuarioId}", id, ObterUsuarioLogadoId());

            // ✅ VERIFICAR PERMISSÃO
            if (!TemPermissao("Permissoes.Editar"))
            {
                return Forbid("Usuário não tem permissão para reativar permissões");
            }

            // ✅ BUSCAR PERMISSÃO
            var permissao = await _context.Permissoes.FindAsync(id);
            if (permissao == null)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "PermissaoNaoEncontrada",
                    Mensagem = $"Permissão com ID {id} não foi encontrada"
                });
            }

            // ✅ VERIFICAR SE JÁ ESTÁ ATIVA
            if (permissao.Ativo)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "PermissaoJaAtiva",
                    Mensagem = "A permissão já está ativa"
                });
            }

            // ✅ REATIVAR
            permissao.Ativo = true;
            await _context.SaveChangesAsync();

            // ✅ REGISTRAR AUDITORIA
            await RegistrarAuditoria("Reativar", "Permissao", id.ToString(),
                $"Permissão '{permissao.Nome}' reativada", new { Ativo = false }, new { Ativo = true });

            _logger.LogInformation("✅ Permissão {Id} reativada com sucesso", id);

            // ✅ RETORNAR DADOS COMPLETOS
            var permissaoCompleta = await ObterPermissaoCompletaAsync(id);
            return Ok(permissaoCompleta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao reativar permissão {Id}", id);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno do servidor ao reativar permissão"
            });
        }
    }

    /// <summary>
    /// Lista todas as categorias de permissões disponíveis
    /// </summary>
    /// <returns>Lista de categorias</returns>
    [HttpGet("categorias")]
    [ProducesResponseType(typeof(List<CategoriaPermissao>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarCategorias()
    {
        try
        {
            _logger.LogInformation("📋 Listando categorias de permissões - Usuário: {UsuarioId}", ObterUsuarioLogadoId());

            // ✅ VERIFICAR PERMISSÃO
            if (!TemPermissao("Permissoes.Listar"))
            {
                return Forbid("Usuário não tem permissão para listar categorias de permissões");
            }

            // ✅ BUSCAR CATEGORIAS COM ESTATÍSTICAS
            var categorias = await _context.Permissoes
                .Where(p => p.Categoria != null)
                .GroupBy(p => p.Categoria)
                .Select(g => new CategoriaPermissao
                {
                    Nome = g.Key!,
                    TotalPermissoes = g.Count(),
                    PermissoesAtivas = g.Count(p => p.Ativo),
                    PermissoesInativas = g.Count(p => !p.Ativo)
                })
                .OrderBy(c => c.Nome)
                .ToListAsync();

            _logger.LogInformation("✅ {Total} categorias encontradas", categorias.Count);
            return Ok(categorias);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar categorias");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno do servidor ao listar categorias"
            });
        }
    }
    
    #region Métodos Auxiliares

    /// <summary>
    /// Aplica ordenação dinâmica à query de permissões
    /// </summary>
    /// <param name="query">Query base</param>
    /// <param name="ordenarPor">Campo para ordenação</param>
    /// <param name="direcaoOrdenacao">Direção da ordenação (asc/desc)</param>
    /// <returns>Query com ordenação aplicada</returns>
    private IQueryable<Permissao> AplicarOrdenacao(IQueryable<Permissao> query, string? ordenarPor, string? direcaoOrdenacao)
    {
        if (string.IsNullOrEmpty(ordenarPor))
        {
            // Ordenação padrão: Nome crescente
            return query.OrderBy(p => p.Nome);
        }

        var crescente = string.IsNullOrEmpty(direcaoOrdenacao) || direcaoOrdenacao.ToLower() == "asc";

        return ordenarPor.ToLower() switch
        {
            "nome" => crescente ? query.OrderBy(p => p.Nome) : query.OrderByDescending(p => p.Nome),
            "descricao" => crescente ? query.OrderBy(p => p.Descricao) : query.OrderByDescending(p => p.Descricao),
            "recurso" => crescente ? query.OrderBy(p => p.Recurso) : query.OrderByDescending(p => p.Recurso),
            "acao" => crescente ? query.OrderBy(p => p.Acao) : query.OrderByDescending(p => p.Acao),
            "categoria" => crescente ? query.OrderBy(p => p.Categoria) : query.OrderByDescending(p => p.Categoria),
            "ativo" => crescente ? query.OrderBy(p => p.Ativo) : query.OrderByDescending(p => p.Ativo),
            "datacriacao" => crescente ? query.OrderBy(p => p.DataCriacao) : query.OrderByDescending(p => p.DataCriacao),
            "totalpapeis" => crescente 
                ? query.OrderBy(p => p.PapelPermissoes.Count(pp => pp.Ativo))
                : query.OrderByDescending(p => p.PapelPermissoes.Count(pp => pp.Ativo)),
            _ => query.OrderBy(p => p.Nome) // Fallback para ordenação padrão
        };
    }

    /// <summary>
    /// Obtém dados completos de uma permissão com relacionamentos
    /// </summary>
    /// <param name="permissaoId">ID da permissão</param>
    /// <returns>Dados completos da permissão ou null se não encontrada</returns>
    private async Task<PermissaoCompleta?> ObterPermissaoCompletaAsync(int permissaoId)
    {
        var permissao = await _context.Permissoes
            .Include(p => p.PapelPermissoes.Where(pp => pp.Ativo))
                .ThenInclude(pp => pp.Papel)
            .FirstOrDefaultAsync(p => p.Id == permissaoId);

        if (permissao == null)
        {
            return null;
        }

        // Mapear para DTO
        return new PermissaoCompleta
        {
            Id = permissao.Id,
            Nome = permissao.Nome,
            Descricao = permissao.Descricao,
            Recurso = permissao.Recurso,
            Acao = permissao.Acao,
            Categoria = permissao.Categoria,
            Ativo = permissao.Ativo,
            DataCriacao = permissao.DataCriacao,
            TotalPapeis = permissao.PapelPermissoes.Count(pp => pp.Ativo),
            Papeis = permissao.PapelPermissoes
                .Where(pp => pp.Ativo)
                .Select(pp => new PapelPermissaoResumo
                {
                    Id = pp.Papel.Id,
                    Nome = pp.Papel.Name!,
                    Descricao = pp.Papel.Descricao,
                    Categoria = pp.Papel.Categoria,
                    Nivel = pp.Papel.Nivel,
                    DataAtribuicao = pp.DataAtribuicao,
                    Ativo = pp.Papel.Ativo
                })
                .OrderBy(p => p.Nome)
                .ToList(),
            Estatisticas = new EstatisticasPermissao
            {
                TotalPapeisAtivos = permissao.PapelPermissoes.Count(pp => pp.Ativo && pp.Papel.Ativo),
                TotalPapeisInativos = permissao.PapelPermissoes.Count(pp => pp.Ativo && !pp.Papel.Ativo),
                TotalUsuariosComPermissao = await ContarUsuariosComPermissao(permissaoId),
                UltimaAtribuicao = permissao.PapelPermissoes
                    .Where(pp => pp.Ativo)
                    .Max(pp => (DateTime?)pp.DataAtribuicao)
            }
        };
    }

    /// <summary>
    /// Conta quantos usuários têm uma permissão específica através dos papéis
    /// </summary>
    /// <param name="permissaoId">ID da permissão</param>
    /// <returns>Número de usuários com a permissão</returns>
    private async Task<int> ContarUsuariosComPermissao(int permissaoId)
    {
        return await _context.UsuarioPapeis
            .Where(up => up.Ativo && 
                            up.Papel.Ativo && 
                            up.Usuario.Ativo &&
                            up.Papel.PapelPermissoes.Any(pp => pp.PermissaoId == permissaoId && pp.Ativo))
            .Select(up => up.UsuarioId)
            .Distinct()
            .CountAsync();
    }

    /// <summary>
    /// Obtém ID do usuário logado a partir do token JWT
    /// </summary>
    /// <returns>ID do usuário logado</returns>
    private int ObterUsuarioLogadoId()
    {
        var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                            User.FindFirstValue("sub") ?? 
                            User.FindFirstValue("id");
        
        if (string.IsNullOrEmpty(usuarioIdClaim) || !int.TryParse(usuarioIdClaim, out var usuarioId))
        {
            throw new UnauthorizedAccessException("ID do usuário não encontrado no token");
        }

        return usuarioId;
    }

    /// <summary>
    /// Verifica se o usuário logado tem uma permissão específica
    /// </summary>
    /// <param name="permissao">Nome da permissão a verificar</param>
    /// <returns>True se o usuário tem a permissão</returns>
    private bool TemPermissao(string permissao)
    {
        try
        {
            // Verificar se é SuperAdmin (tem todas as permissões)
            if (User.IsInRole("SuperAdmin"))
            {
                return true;
            }

            // Verificar permissão específica via claims
            var permissoesClaim = User.FindFirstValue("permissions") ?? User.FindFirstValue("permissoes");
            
            if (!string.IsNullOrEmpty(permissoesClaim))
            {
                var permissoesUsuario = permissoesClaim.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                        .Select(p => p.Trim())
                                                        .ToHashSet();
                
                return permissoesUsuario.Contains(permissao);
            }

            // Fallback: verificar permissões via papéis (consulta ao banco)
            var usuarioId = ObterUsuarioLogadoId();
            return VerificarPermissaoViaPapeis(usuarioId, permissao);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao verificar permissão {Permissao} para usuário", permissao);
            return false;
        }
    }

    /// <summary>
    /// Verifica permissão consultando diretamente os papéis do usuário no banco
    /// </summary>
    /// <param name="usuarioId">ID do usuário</param>
    /// <param name="permissao">Nome da permissão</param>
    /// <returns>True se o usuário tem a permissão</returns>
    private bool VerificarPermissaoViaPapeis(int usuarioId, string permissao)
    {
        try
        {
            return _context.UsuarioPapeis
                .Where(up => up.UsuarioId == usuarioId && 
                            up.Ativo && 
                            up.Papel.Ativo)
                .Any(up => up.Papel.PapelPermissoes
                    .Any(pp => pp.Permissao.Nome == permissao && 
                                pp.Ativo && 
                                pp.Permissao.Ativo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar permissão {Permissao} via papéis para usuário {UsuarioId}", 
                permissao, usuarioId);
            return false;
        }
    }

    /// <summary>
    /// Registra uma ação na auditoria do sistema
    /// </summary>
    /// <param name="acao">Ação realizada</param>
    /// <param name="recurso">Tipo de recurso</param>
    /// <param name="recursoId">ID do recurso</param>
    /// <param name="observacoes">Observações adicionais</param>
    /// <param name="dadosAntes">Dados antes da alteração</param>
    /// <param name="dadosDepois">Dados depois da alteração</param>
    private async Task RegistrarAuditoria(string acao, string recurso, string? recursoId, 
        string observacoes, object? dadosAntes = null, object? dadosDepois = null)
    {
        try
        {
            var usuarioId = ObterUsuarioLogadoId();
            var httpContext = HttpContext;

            var registro = new RegistroAuditoria
            {
                UsuarioId = usuarioId,
                Acao = acao,
                Recurso = recurso,
                RecursoId = recursoId,
                DataHora = DateTime.UtcNow,
                EnderecoIp = ObterEnderecoIp(httpContext),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                DadosAntes = dadosAntes != null ? JsonSerializer.Serialize(dadosAntes) : null,
                DadosDepois = dadosDepois != null ? JsonSerializer.Serialize(dadosDepois) : null,
                Observacoes = observacoes
            };

            _context.RegistrosAuditoria.Add(registro);
            await _context.SaveChangesAsync();

            _logger.LogInformation("📝 Auditoria registrada: {Acao} em {Recurso} por usuário {UsuarioId}", 
                acao, recurso, usuarioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao registrar auditoria: {Acao} em {Recurso}", acao, recurso);
            // Não fazer throw para não interromper a operação principal
        }
    }

    /// <summary>
    /// Obtém o endereço IP da requisição considerando proxies
    /// </summary>
    /// <param name="httpContext">Contexto HTTP da requisição</param>
    /// <returns>Endereço IP do cliente</returns>
    private static string ObterEnderecoIp(HttpContext httpContext)
    {
        // Verificar cabeçalhos de proxy primeiro
        var xForwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            // Pegar o primeiro IP da lista (IP original do cliente)
            var ips = xForwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Verificar cabeçalho X-Real-IP
        var xRealIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        // Fallback para RemoteIpAddress
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconhecido";
    }

    /// <summary>
    /// Valida se uma permissão pode ser criada/atualizada
    /// </summary>
    /// <param name="nome">Nome da permissão</param>
    /// <param name="recurso">Recurso da permissão</param>
    /// <param name="acao">Ação da permissão</param>
    /// <param name="permissaoIdExcluir">ID da permissão a excluir da validação (para updates)</param>
    /// <returns>Resultado da validação</returns>
    private async Task<ResultadoValidacao> ValidarPermissao(string nome, string recurso, string acao, int? permissaoIdExcluir = null)
    {
        var erros = new List<string>();

        // Validar nome único
        var permissaoComNome = await _context.Permissoes
            .FirstOrDefaultAsync(p => p.Nome == nome && (permissaoIdExcluir == null || p.Id != permissaoIdExcluir));

        if (permissaoComNome != null)
        {
            erros.Add($"Já existe uma permissão com o nome '{nome}'");
        }

        // Validar combinação recurso+ação única
        var permissaoComRecursoAcao = await _context.Permissoes
            .FirstOrDefaultAsync(p => p.Recurso == recurso && p.Acao == acao && 
                                (permissaoIdExcluir == null || p.Id != permissaoIdExcluir));

        if (permissaoComRecursoAcao != null)
        {
            erros.Add($"Já existe uma permissão para o recurso '{recurso}' com a ação '{acao}'");
        }

        // Validar padrão de nomenclatura
        if (!nome.Contains('.'))
        {
            erros.Add("Nome da permissão deve seguir o padrão 'Recurso.Acao'");
        }

        // Validar se segue convenções
        var partesNome = nome.Split('.');
        if (partesNome.Length >= 2)
        {
            var recursoNome = partesNome[0];
            var acaoNome = partesNome[1];

            if (!string.Equals(recursoNome, recurso, StringComparison.OrdinalIgnoreCase))
            {
                erros.Add($"O recurso no nome '{recursoNome}' deve corresponder ao campo recurso '{recurso}'");
            }

            if (!string.Equals(acaoNome, acao, StringComparison.OrdinalIgnoreCase))
            {
                erros.Add($"A ação no nome '{acaoNome}' deve corresponder ao campo ação '{acao}'");
            }
        }

        return new ResultadoValidacao
        {
            Valido = !erros.Any(),
            Erros = erros
        };
    }

    #endregion

    #region Classes Auxiliares

    /// <summary>
    /// Resultado de validação
    /// </summary>
    private class ResultadoValidacao
    {
        public bool Valido { get; set; }
        public List<string> Erros { get; set; } = new();
    }

    #endregion
}