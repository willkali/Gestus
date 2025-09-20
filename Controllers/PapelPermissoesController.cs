using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Gestus.Modelos;
using Gestus.Dados;
using Gestus.DTOs.PapelPermissao;
using Gestus.DTOs.Comuns;
using System.Text.Json;

namespace Gestus.Controllers;

/// <summary>
/// Controller para gerenciamento avançado de associações papel-permissão
/// Implementa operações de associação, relatórios e análises
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class PapelPermissoesController : ControllerBase
{
    private readonly GestusDbContexto _context;
    private readonly ILogger<PapelPermissoesController> _logger;

    public PapelPermissoesController(
        GestusDbContexto context,
        ILogger<PapelPermissoesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as associações papel-permissão com filtros
    /// </summary>
    /// <param name="filtros">Filtros de busca</param>
    /// <returns>Lista paginada de associações</returns>
    [HttpGet]
    [ProducesResponseType(typeof(RespostaPaginada<AssociacaoPapelPermissao>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarAssociacoes([FromQuery] FiltrosAssociacoes filtros)
    {
        var usuarioLogadoId = ObterUsuarioLogadoId();
        _logger.LogInformation("🔍 Usuario {UsuarioId} tentando acessar ListarAssociacoes", usuarioLogadoId);

        // ✅ LOG DETALHADO DE DEBUG
        _logger.LogInformation("👤 Claims do usuário:");
        foreach (var claim in User.Claims.Take(10))
        {
            _logger.LogInformation("   - {Type}: {Value}", claim.Type, claim.Value);
        }

        if (!TemPermissao("Papeis.GerenciarPermissoes"))
        {
            return StatusCode(403, new RespostaErro 
            { 
                Erro = "Acesso negado", 
                Mensagem = "Acesso negado para visualizar associações papel-permissão" 
            });
        }

        try
        {
            _logger.LogInformation("🔍 Usuario {UsuarioId} listando associações papel-permissão", usuarioLogadoId);

            // ✅ QUERY BASE com relacionamentos otimizados
            var query = _context.PapelPermissoes
                .Include(pp => pp.Papel)
                .Include(pp => pp.Permissao)
                .AsQueryable();

            // ✅ APLICAR FILTROS ESPECÍFICOS
            query = AplicarFiltros(query, filtros);

            // ✅ APLICAR ORDENAÇÃO
            query = AplicarOrdenacao(query, filtros.OrdenarPor, filtros.DirecaoOrdenacao);

            // ✅ CONTAR TOTAL ANTES DA PAGINAÇÃO
            var totalItens = await query.CountAsync();

            // ✅ APLICAR PAGINAÇÃO
            var associacoes = await query
                .Skip((filtros.Pagina - 1) * filtros.ItensPorPagina)
                .Take(filtros.ItensPorPagina)
                .Select(pp => new AssociacaoPapelPermissao
                {
                    PapelId = pp.PapelId,
                    PapelNome = pp.Papel.Name ?? "N/A",
                    PermissaoId = pp.PermissaoId,
                    PermissaoNome = pp.Permissao.Nome,
                    DataAtribuicao = pp.DataAtribuicao,
                    Ativo = pp.Ativo
                })
                .ToListAsync();

            // ✅ CALCULAR DADOS DE PAGINAÇÃO
            var totalPaginas = (int)Math.Ceiling((double)totalItens / filtros.ItensPorPagina);

            var resposta = new RespostaPaginada<AssociacaoPapelPermissao>
            {
                Dados = associacoes,
                PaginaAtual = filtros.Pagina,
                ItensPorPagina = filtros.ItensPorPagina,
                TotalItens = totalItens,
                TotalPaginas = totalPaginas,
                TemProximaPagina = filtros.Pagina < totalPaginas,
                TemPaginaAnterior = filtros.Pagina > 1
            };

            _logger.LogInformation("✅ Listagem concluída: {Total} associações encontradas", totalItens);
            return Ok(resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar associações papel-permissão");
            return StatusCode(500, new RespostaErro
            {
                Erro = "Erro interno",
                Mensagem = "Erro ao processar solicitação de listagem"
            });
        }
    }

    /// <summary>
    /// Obtém uma associação específica por IDs do papel e permissão
    /// </summary>
    /// <param name="papelId">ID do papel</param>
    /// <param name="permissaoId">ID da permissão</param>
    /// <returns>Detalhes da associação</returns>
    [HttpGet("{papelId:int}/{permissaoId:int}")]
    [ProducesResponseType(typeof(AssociacaoPapelPermissao), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterAssociacaoPorIds(int papelId, int permissaoId)
    {
        if (!TemPermissao("Papeis.GerenciarPermissoes"))
        {
            return StatusCode(403, new RespostaErro 
            { 
                Erro = "Acesso negado", 
                Mensagem = "Acesso negado para visualizar associação específica" 
            });
        }

        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            _logger.LogInformation("🔍 Usuario {UsuarioId} consultando associação papel {PapelId} - permissão {PermissaoId}",
                usuarioLogadoId, papelId, permissaoId);

            var associacao = await _context.PapelPermissoes
                .Include(pp => pp.Papel)
                .Include(pp => pp.Permissao)
                .Where(pp => pp.PapelId == papelId && pp.PermissaoId == permissaoId)
                .Select(pp => new AssociacaoPapelPermissao
                {
                    PapelId = pp.PapelId,
                    PapelNome = pp.Papel.Name ?? "N/A",
                    PermissaoId = pp.PermissaoId,
                    PermissaoNome = pp.Permissao.Nome,
                    DataAtribuicao = pp.DataAtribuicao,
                    Ativo = pp.Ativo
                })
                .FirstOrDefaultAsync();

            if (associacao == null)
            {
                _logger.LogWarning("⚠️ Associação não encontrada: papel {PapelId} - permissão {PermissaoId}", papelId, permissaoId);
                return NotFound(new RespostaErro
                {
                    Erro = "Associação não encontrada",
                    Mensagem = $"Não existe associação entre papel {papelId} e permissão {permissaoId}"
                });
            }

            _logger.LogInformation("✅ Associação encontrada: {PapelNome} - {PermissaoNome}",
                associacao.PapelNome, associacao.PermissaoNome);

            return Ok(associacao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter associação papel {PapelId} - permissão {PermissaoId}", papelId, permissaoId);
            return StatusCode(500, new RespostaErro
            {
                Erro = "Erro interno",
                Mensagem = "Erro ao consultar associação específica"
            });
        }
    }

    /// <summary>
    /// Gera relatório detalhado de permissões por papel
    /// </summary>
    /// <param name="papelId">ID do papel para análise</param>
    /// <param name="incluirComparacao">Incluir comparação com outros papéis</param>
    /// <returns>Relatório completo do papel</returns>
    [HttpGet("relatorio/papel/{papelId:int}")]
    [ProducesResponseType(typeof(RelatorioPermissoesPapel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterRelatorioPermissoesPapel(int papelId, [FromQuery] bool incluirComparacao = true)
    {
        if (!TemPermissao("Papeis.GerenciarPermissoes"))
        {
            return StatusCode(403, new RespostaErro 
            { 
                Erro = "Acesso negado", 
                Mensagem = "Acesso negado para gerar relatórios de papel-permissão" 
            });
        }

        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            _logger.LogInformation("📊 Usuario {UsuarioId} gerando relatório de permissões do papel {PapelId}",
                usuarioLogadoId, papelId);

            // ✅ VERIFICAR SE PAPEL EXISTE
            var papel = await _context.Papeis.FindAsync(papelId);
            if (papel == null)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "Papel não encontrado",
                    Mensagem = $"Papel com ID {papelId} não existe"
                });
            }

            // ✅ OBTER PERMISSÕES DO PAPEL
            var permissoesPapel = await _context.PapelPermissoes
                .Include(pp => pp.Permissao)
                .Where(pp => pp.PapelId == papelId)
                .ToListAsync();

            // ✅ CONTAR OUTRAS ASSOCIAÇÕES PARA CADA PERMISSÃO
            var permissoesDetalhadas = new List<PermissaoDetalhada>();

            foreach (var pp in permissoesPapel)
            {
                var outrosPapeis = await _context.PapelPermissoes
                    .Where(x => x.PermissaoId == pp.PermissaoId && x.PapelId != papelId)
                    .CountAsync();

                permissoesDetalhadas.Add(new PermissaoDetalhada
                {
                    Id = pp.Permissao.Id,
                    Nome = pp.Permissao.Nome,
                    Categoria = pp.Permissao.Categoria,
                    DataAtribuicao = pp.DataAtribuicao,
                    Ativo = pp.Ativo,
                    OutrosPapeisComPermissao = outrosPapeis
                });
            }

            // ✅ CONSTRUIR RELATÓRIO BASE
            var relatorio = new RelatorioPermissoesPapel
            {
                Papel = new PapelResumoRelatorio
                {
                    Id = papel.Id,
                    Nome = papel.Name ?? "N/A",
                    Descricao = papel.Descricao,
                    Categoria = papel.Categoria,
                    Nivel = papel.Nivel,
                    Ativo = papel.Ativo
                },
                TotalPermissoes = permissoesPapel.Count,
                PermissoesAtivas = permissoesPapel.Count(p => p.Ativo),
                PermissoesInativas = permissoesPapel.Count(p => !p.Ativo),
                Permissoes = permissoesDetalhadas
            };

            // ✅ INCLUIR COMPARAÇÃO SE SOLICITADO
            if (incluirComparacao)
            {
                relatorio.Comparacao = await GerarComparacaoPapeis(papelId);
            }

            _logger.LogInformation("✅ Relatório gerado: papel {PapelNome} com {Total} permissões",
                papel.Name, permissoesPapel.Count);

            return Ok(relatorio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao gerar relatório de permissões do papel {PapelId}", papelId);
            return StatusCode(500, new RespostaErro
            {
                Erro = "Erro interno",
                Mensagem = "Erro ao gerar relatório de permissões"
            });
        }
    }

    /// <summary>
    /// Lista todas as permissões disponíveis que ainda não estão associadas a um papel
    /// </summary>
    /// <param name="papelId">ID do papel para verificar permissões disponíveis</param>
    /// <returns>Lista de permissões disponíveis</returns>
    [HttpGet("permissoes/disponiveis/{papelId:int}")]
    [ProducesResponseType(typeof(List<PermissaoDetalhada>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarPermissoesDisponiveis(int papelId)
    {
        if (!TemPermissao("Papeis.GerenciarPermissoes"))
        {
            return StatusCode(403, new RespostaErro 
            { 
                Erro = "Acesso negado", 
                Mensagem = "Acesso negado para listar permissões disponíveis" 
            });
        }

        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            _logger.LogInformation("🔍 Usuario {UsuarioId} listando permissões disponíveis para papel {PapelId}",
                usuarioLogadoId, papelId);

            // ✅ VERIFICAR SE PAPEL EXISTE
            var papelExiste = await _context.Papeis.AnyAsync(p => p.Id == papelId);
            if (!papelExiste)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "Papel não encontrado",
                    Mensagem = $"Papel com ID {papelId} não existe"
                });
            }

            // ✅ OBTER PERMISSÕES QUE O PAPEL NÃO TEM
            var permissoesJaAssociadas = _context.PapelPermissoes
                .Where(pp => pp.PapelId == papelId)
                .Select(pp => pp.PermissaoId);

            var permissoesDisponiveis = await _context.Permissoes
                .Where(p => p.Ativo && !permissoesJaAssociadas.Contains(p.Id))
                .Select(p => new PermissaoDetalhada
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Categoria = p.Categoria,
                    DataAtribuicao = DateTime.UtcNow, // Será a data se for atribuída
                    Ativo = p.Ativo,
                    OutrosPapeisComPermissao = _context.PapelPermissoes
                        .Count(pp => pp.PermissaoId == p.Id && pp.Ativo)
                })
                .OrderBy(p => p.Categoria)
                .ThenBy(p => p.Nome)
                .ToListAsync();

            _logger.LogInformation("✅ Encontradas {Total} permissões disponíveis para o papel {PapelId}",
                permissoesDisponiveis.Count, papelId);

            return Ok(permissoesDisponiveis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar permissões disponíveis para papel {PapelId}", papelId);
            return StatusCode(500, new RespostaErro
            {
                Erro = "Erro interno",
                Mensagem = "Erro ao consultar permissões disponíveis"
            });
        }
    }

    /// <summary>
    /// Associa uma permissão específica a um papel
    /// </summary>
    /// <param name="papelId">ID do papel</param>
    /// <param name="permissaoId">ID da permissão</param>
    /// <returns>Confirmação da associação</returns>
    [HttpPost("{papelId:int}/permissoes/{permissaoId:int}")]
    [ProducesResponseType(typeof(AssociacaoPapelPermissao), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssociarPermissaoAoPapel(int papelId, int permissaoId)
    {
        if (!TemPermissao("Papeis.GerenciarPermissoes"))
        {
            return StatusCode(403, new RespostaErro 
            { 
                Erro = "Acesso negado", 
                Mensagem = "Acesso negado para associar permissões a papéis" 
            });
        }

        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            _logger.LogInformation("🔗 Usuario {UsuarioId} associando permissão {PermissaoId} ao papel {PapelId}",
                usuarioLogadoId, permissaoId, papelId);

            // ✅ VERIFICAR SE PAPEL EXISTE E ESTÁ ATIVO
            var papel = await _context.Papeis.FindAsync(papelId);
            if (papel == null || !papel.Ativo)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "Papel não encontrado",
                    Mensagem = $"Papel com ID {papelId} não existe ou está inativo"
                });
            }

            // ✅ VERIFICAR SE PERMISSÃO EXISTE E ESTÁ ATIVA
            var permissao = await _context.Permissoes.FindAsync(permissaoId);
            if (permissao == null || !permissao.Ativo)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "Permissão não encontrada",
                    Mensagem = $"Permissão com ID {permissaoId} não existe ou está inativa"
                });
            }

            // ✅ VERIFICAR SE ASSOCIAÇÃO JÁ EXISTE
            var associacaoExistente = await _context.PapelPermissoes
                .FirstOrDefaultAsync(pp => pp.PapelId == papelId && pp.PermissaoId == permissaoId);

            if (associacaoExistente != null)
            {
                if (associacaoExistente.Ativo)
                {
                    return BadRequest(new RespostaErro
                    {
                        Erro = "Associação já existe",
                        Mensagem = $"O papel '{papel.Name}' já possui a permissão '{permissao.Nome}'"
                    });
                }
                else
                {
                    // ✅ REATIVAR ASSOCIAÇÃO EXISTENTE
                    associacaoExistente.Ativo = true;
                    associacaoExistente.DataAtribuicao = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("✅ Associação reativada: {PapelNome} - {PermissaoNome}",
                        papel.Name, permissao.Nome);
                }
            }
            else
            {
                // ✅ CRIAR NOVA ASSOCIAÇÃO
                var novaAssociacao = new PapelPermissao
                {
                    PapelId = papelId,
                    PermissaoId = permissaoId,
                    DataAtribuicao = DateTime.UtcNow,
                    Ativo = true
                };

                _context.PapelPermissoes.Add(novaAssociacao);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Nova associação criada: {PapelNome} - {PermissaoNome}",
                    papel.Name, permissao.Nome);
            }

            // ✅ RETORNAR DADOS DA ASSOCIAÇÃO
            var resultado = new AssociacaoPapelPermissao
            {
                PapelId = papelId,
                PapelNome = papel.Name ?? "N/A",
                PermissaoId = permissaoId,
                PermissaoNome = permissao.Nome,
                DataAtribuicao = DateTime.UtcNow,
                Ativo = true
            };

            return CreatedAtAction(nameof(ObterAssociacaoPorIds),
                new { papelId, permissaoId }, resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao associar permissão {PermissaoId} ao papel {PapelId}",
                permissaoId, papelId);
            return StatusCode(500, new RespostaErro
            {
                Erro = "Erro interno",
                Mensagem = "Erro ao criar associação papel-permissão"
            });
        }
    }

    /// <summary>
    /// Remove associação entre papel e permissão
    /// </summary>
    /// <param name="papelId">ID do papel</param>
    /// <param name="permissaoId">ID da permissão</param>
    /// <param name="exclusaoPermanente">Se true, remove definitivamente; se false, apenas desativa</param>
    /// <returns>Confirmação da remoção</returns>
    [HttpDelete("{papelId:int}/permissoes/{permissaoId:int}")]
    [ProducesResponseType(typeof(RespostaSucesso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DissociarPermissaoDoPapel(int papelId, int permissaoId, [FromQuery] bool exclusaoPermanente = false)
    {
        if (!TemPermissao("Papeis.GerenciarPermissoes"))
        {
            return StatusCode(403, new RespostaErro 
            { 
                Erro = "Acesso negado", 
                Mensagem = "Acesso negado para remover associações papel-permissão" 
            });
        }

        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            _logger.LogInformation("🔗 Usuario {UsuarioId} removendo associação papel {PapelId} - permissão {PermissaoId}",
                usuarioLogadoId, papelId, permissaoId);

            // ✅ VERIFICAR SE ASSOCIAÇÃO EXISTE
            var associacao = await _context.PapelPermissoes
                .Include(pp => pp.Papel)
                .Include(pp => pp.Permissao)
                .FirstOrDefaultAsync(pp => pp.PapelId == papelId && pp.PermissaoId == permissaoId);

            if (associacao == null)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "Associação não encontrada",
                    Mensagem = $"Não existe associação entre papel {papelId} e permissão {permissaoId}"
                });
            }

            string operacao;
            if (exclusaoPermanente)
            {
                // ✅ EXCLUSÃO PERMANENTE
                _context.PapelPermissoes.Remove(associacao);
                operacao = "removida permanentemente";
            }
            else
            {
                // ✅ SOFT DELETE (DESATIVAÇÃO)
                associacao.Ativo = false;
                operacao = "desativada";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Associação {Operacao}: {PapelNome} - {PermissaoNome}",
                operacao, associacao.Papel.Name, associacao.Permissao.Nome);

            return Ok(new RespostaSucesso
            {
                Sucesso = true,
                Mensagem = $"Associação entre papel '{associacao.Papel.Name}' e permissão '{associacao.Permissao.Nome}' foi {operacao} com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao remover associação papel {PapelId} - permissão {PermissaoId}",
                papelId, permissaoId);
            return StatusCode(500, new RespostaErro
            {
                Erro = "Erro interno",
                Mensagem = "Erro ao remover associação papel-permissão"
            });
        }
    }

    /// <summary>
    /// Executa operações em lote com associações papel-permissão
    /// </summary>
    /// <param name="request">Dados da operação em lote</param>
    /// <returns>Resultado da operação em lote</returns>
    [HttpPost("lote")]
    [ProducesResponseType(typeof(RespostaAssociacaoLote), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExecutarAssociacaoLote([FromBody] AssociacaoLoteRequest request)
    {
        if (!TemPermissao("Papeis.GerenciarPermissoes"))
        {
            return StatusCode(403, new RespostaErro 
            { 
                Erro = "Acesso negado", 
                Mensagem = "Acesso negado para operações em lote com associações papel-permissão" 
            });
        }

        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            var inicioOperacao = DateTime.UtcNow;

            _logger.LogInformation("🔗 Usuario {UsuarioId} executando operação em lote: {Operacao}",
                usuarioLogadoId, request.Operacao);

            // ✅ VALIDAR OPERAÇÃO
            var operacoesValidas = new[] { "associar", "dissociar", "substituir" };
            if (!operacoesValidas.Contains(request.Operacao.ToLower()))
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "Operação inválida",
                    Mensagem = "Operação deve ser: associar, dissociar ou substituir"
                });
            }

            // ✅ VALIDAR DADOS
            if (!request.PapeisIds.Any() || !request.PermissoesIds.Any())
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "Dados insuficientes",
                    Mensagem = "Deve fornecer pelo menos um papel e uma permissão"
                });
            }

            // ✅ VERIFICAR SE PAPÉIS EXISTEM
            var papeisExistentes = await _context.Papeis
                .Where(p => request.PapeisIds.Contains(p.Id) && p.Ativo)
                .ToListAsync();

            var papeisInvalidos = request.PapeisIds.Except(papeisExistentes.Select(p => p.Id)).ToList();
            if (papeisInvalidos.Any())
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "Papéis inválidos",
                    Mensagem = $"Papéis não encontrados ou inativos: {string.Join(", ", papeisInvalidos)}"
                });
            }

            // ✅ VERIFICAR SE PERMISSÕES EXISTEM
            var permissoesExistentes = await _context.Permissoes
                .Where(p => request.PermissoesIds.Contains(p.Id) && p.Ativo)
                .ToListAsync();

            var permissoesInvalidas = request.PermissoesIds.Except(permissoesExistentes.Select(p => p.Id)).ToList();
            if (permissoesInvalidas.Any())
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "Permissões inválidas",
                    Mensagem = $"Permissões não encontradas ou inativas: {string.Join(", ", permissoesInvalidas)}"
                });
            }

            // ✅ EXECUTAR OPERAÇÃO ESPECÍFICA
            var resultado = request.Operacao.ToLower() switch
            {
                "associar" => await ExecutarAssociacaoEmLote(papeisExistentes, permissoesExistentes),
                "dissociar" => await ExecutarDissociacaoEmLote(papeisExistentes, permissoesExistentes),
                "substituir" => await ExecutarSubstituicaoEmLote(papeisExistentes, permissoesExistentes),
                _ => throw new InvalidOperationException("Operação não implementada")
            };

            resultado.Duracao = DateTime.UtcNow - inicioOperacao;

            _logger.LogInformation("✅ Operação em lote concluída: {TotalSucessos}/{TotalProcessadas} sucessos",
                resultado.TotalSucessos, resultado.TotalProcessadas);

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao executar operação em lote de associações");
            return StatusCode(500, new RespostaErro
            {
                Erro = "Erro interno",
                Mensagem = "Erro ao executar operação em lote"
            });
        }
    }

    /// <summary>
    /// Substitui todas as permissões de um papel por uma nova lista
    /// </summary>
    /// <param name="papelId">ID do papel</param>
    /// <param name="permissoesIds">Lista de IDs das novas permissões</param>
    /// <returns>Lista atualizada de permissões do papel</returns>
    [HttpPut("{papelId:int}/permissoes")]
    [ProducesResponseType(typeof(List<PermissaoDetalhada>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SubstituirPermissoesDoPapel(int papelId, [FromBody] List<int> permissoesIds)
    {
        if (!TemPermissao("Papeis.GerenciarPermissoes"))
        {
            return StatusCode(403, new RespostaErro 
            { 
                Erro = "Acesso negado", 
                Mensagem = "Acesso negado para substituir permissões de papel" 
            });
        }

        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            _logger.LogInformation("🔄 Usuario {UsuarioId} substituindo permissões do papel {PapelId}",
                usuarioLogadoId, papelId);

            // ✅ VERIFICAR SE PAPEL EXISTE
            var papel = await _context.Papeis.FindAsync(papelId);
            if (papel == null || !papel.Ativo)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "Papel não encontrado",
                    Mensagem = $"Papel com ID {papelId} não existe ou está inativo"
                });
            }

            // ✅ VERIFICAR SE PERMISSÕES EXISTEM
            var permissoesValidas = await _context.Permissoes
                .Where(p => permissoesIds.Contains(p.Id) && p.Ativo)
                .ToListAsync();

            var permissoesInvalidas = permissoesIds.Except(permissoesValidas.Select(p => p.Id)).ToList();
            if (permissoesInvalidas.Any())
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "Permissões inválidas",
                    Mensagem = $"Permissões não encontradas ou inativas: {string.Join(", ", permissoesInvalidas)}"
                });
            }

            // ✅ REMOVER TODAS AS ASSOCIAÇÕES ATUAIS (SOFT DELETE)
            var associacoesAtuais = await _context.PapelPermissoes
                .Where(pp => pp.PapelId == papelId && pp.Ativo)
                .ToListAsync();

            foreach (var associacao in associacoesAtuais)
            {
                associacao.Ativo = false;
            }

            // ✅ CRIAR NOVAS ASSOCIAÇÕES
            var novasAssociacoes = new List<PapelPermissao>();
            foreach (var permissaoId in permissoesIds)
            {
                // Verificar se já existe associação inativa para reativar
                var associacaoExistente = await _context.PapelPermissoes
                    .FirstOrDefaultAsync(pp => pp.PapelId == papelId && pp.PermissaoId == permissaoId);

                if (associacaoExistente != null)
                {
                    associacaoExistente.Ativo = true;
                    associacaoExistente.DataAtribuicao = DateTime.UtcNow;
                }
                else
                {
                    novasAssociacoes.Add(new PapelPermissao
                    {
                        PapelId = papelId,
                        PermissaoId = permissaoId,
                        DataAtribuicao = DateTime.UtcNow,
                        Ativo = true
                    });
                }
            }

            _context.PapelPermissoes.AddRange(novasAssociacoes);
            await _context.SaveChangesAsync();

            // ✅ RETORNAR LISTA ATUALIZADA
            var permissoesAtualizadas = await _context.PapelPermissoes
                .Include(pp => pp.Permissao)
                .Where(pp => pp.PapelId == papelId && pp.Ativo)
                .Select(pp => new PermissaoDetalhada
                {
                    Id = pp.Permissao.Id,
                    Nome = pp.Permissao.Nome,
                    Categoria = pp.Permissao.Categoria,
                    DataAtribuicao = pp.DataAtribuicao,
                    Ativo = pp.Ativo,
                    OutrosPapeisComPermissao = _context.PapelPermissoes
                        .Count(x => x.PermissaoId == pp.PermissaoId && x.PapelId != papelId && x.Ativo)
                })
                .ToListAsync();

            _logger.LogInformation("✅ Permissões substituídas: papel {PapelNome} agora tem {Total} permissões",
                papel.Name, permissoesAtualizadas.Count);

            return Ok(permissoesAtualizadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao substituir permissões do papel {PapelId}", papelId);
            return StatusCode(500, new RespostaErro
            {
                Erro = "Erro interno",
                Mensagem = "Erro ao substituir permissões do papel"
            });
        }
    }

    /// <summary>
    /// Gera relatório completo do sistema de permissões por papéis
    /// </summary>
    /// <param name="incluirEstatisticas">Incluir estatísticas detalhadas</param>
    /// <param name="incluirComparacoes">Incluir comparações entre papéis</param>
    /// <returns>Relatório completo do sistema</returns>
    [HttpGet("relatorio/sistema")]
    [ProducesResponseType(typeof(RelatorioCompletoSistema), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GerarRelatorioCompleto(
        [FromQuery] bool incluirEstatisticas = true,
        [FromQuery] bool incluirComparacoes = true)
    {
        if (!TemPermissao("Sistema.Controle.Total") && !TemPermissao("Papeis.GerenciarPermissoes"))
        {
            return StatusCode(403, new RespostaErro 
            { 
                Erro = "Acesso negado", 
                Mensagem = "Acesso negado para gerar relatórios completos do sistema" 
            });
        }

        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            _logger.LogInformation("📊 Usuario {UsuarioId} gerando relatório completo do sistema", usuarioLogadoId);

            var inicioProcessamento = DateTime.UtcNow;

            // ✅ DADOS BÁSICOS DO SISTEMA
            var totalPapeis = await _context.Papeis.CountAsync();
            var papeisAtivos = await _context.Papeis.CountAsync(p => p.Ativo);
            var totalPermissoes = await _context.Permissoes.CountAsync();
            var permissoesAtivas = await _context.Permissoes.CountAsync(p => p.Ativo);
            var totalAssociacoes = await _context.PapelPermissoes.CountAsync();
            var associacoesAtivas = await _context.PapelPermissoes.CountAsync(pp => pp.Ativo);

            // ✅ ESTATÍSTICAS POR PAPEL
            var estatisticasPorPapel = await _context.PapelPermissoes
                .Include(pp => pp.Papel)
                .Where(pp => pp.Ativo && pp.Papel.Ativo)
                .GroupBy(pp => new { pp.PapelId, pp.Papel.Name, pp.Papel.Categoria })
                .Select(g => new EstatisticaPapel
                {
                    PapelId = g.Key.PapelId,
                    PapelNome = g.Key.Name ?? "N/A",
                    Categoria = g.Key.Categoria,
                    TotalPermissoes = g.Count(),
                    TotalUsuarios = _context.UsuarioPapeis.Count(up => up.PapelId == g.Key.PapelId && up.Ativo)
                })
                .OrderByDescending(e => e.TotalPermissoes)
                .ToListAsync();

            // ✅ ESTATÍSTICAS POR PERMISSÃO
            var estatisticasPorPermissao = await _context.PapelPermissoes
                .Include(pp => pp.Permissao)
                .Where(pp => pp.Ativo && pp.Permissao.Ativo)
                .GroupBy(pp => new { pp.PermissaoId, pp.Permissao.Nome, pp.Permissao.Categoria })
                .Select(g => new EstatisticaPermissao
                {
                    PermissaoId = g.Key.PermissaoId,
                    PermissaoNome = g.Key.Nome,
                    Categoria = g.Key.Categoria,
                    TotalPapeis = g.Count()
                })
                .OrderByDescending(e => e.TotalPapeis)
                .ToListAsync();

            // ✅ DISTRIBUIÇÃO POR CATEGORIA
            var distribuicaoCategorias = await _context.PapelPermissoes
                .Include(pp => pp.Permissao)
                .Where(pp => pp.Ativo && pp.Permissao.Ativo)
                .GroupBy(pp => pp.Permissao.Categoria ?? "Sem Categoria")
                .Select(g => new DistribuicaoCategoria
                {
                    Categoria = g.Key,
                    TotalAssociacoes = g.Count(),
                    TotalPermissoes = g.Select(pp => pp.PermissaoId).Distinct().Count(),
                    TotalPapeis = g.Select(pp => pp.PapelId).Distinct().Count()
                })
                .OrderByDescending(d => d.TotalAssociacoes)
                .ToListAsync();

            var relatorio = new RelatorioCompletoSistema
            {
                ResumoGeral = new ResumoGeral
                {
                    TotalPapeis = totalPapeis,
                    PapeisAtivos = papeisAtivos,
                    TotalPermissoes = totalPermissoes,
                    PermissoesAtivas = permissoesAtivas,
                    TotalAssociacoes = totalAssociacoes,
                    AssociacoesAtivas = associacoesAtivas,
                    DataGeracao = DateTime.UtcNow,
                    TempoProcessamento = DateTime.UtcNow - inicioProcessamento
                },
                EstatisticasPorPapel = estatisticasPorPapel,
                EstatisticasPorPermissao = estatisticasPorPermissao.Take(20).ToList(), // Top 20
                DistribuicaoCategorias = distribuicaoCategorias
            };

            // ✅ INCLUIR ESTATÍSTICAS AVANÇADAS SE SOLICITADO
            if (incluirEstatisticas)
            {
                relatorio.EstatisticasAvancadas = await GerarEstatisticasAvancadas();
            }

            // ✅ INCLUIR COMPARAÇÕES SE SOLICITADO
            if (incluirComparacoes)
            {
                relatorio.Comparacoes = await GerarComparacoesSistema();
            }

            relatorio.ResumoGeral.TempoProcessamento = DateTime.UtcNow - inicioProcessamento;

            _logger.LogInformation("✅ Relatório completo gerado em {Tempo}ms",
                relatorio.ResumoGeral.TempoProcessamento.TotalMilliseconds);

            return Ok(relatorio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao gerar relatório completo do sistema");
            return StatusCode(500, new RespostaErro
            {
                Erro = "Erro interno",
                Mensagem = "Erro ao gerar relatório completo do sistema"
            });
        }
    }

    /// <summary>
    /// Compara permissões entre múltiplos papéis
    /// </summary>
    /// <param name="papeisIds">Lista de IDs dos papéis para comparação</param>
    /// <param name="mostrarApenasConflitos">Mostrar apenas diferenças</param>
    /// <returns>Comparação detalhada entre papéis</returns>
    [HttpPost("comparar-papeis")]
    [ProducesResponseType(typeof(ComparacaoMultiplosPapeis), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CompararPapeis([FromBody] List<int> papeisIds, [FromQuery] bool mostrarApenasConflitos = false)
    {
        if (!TemPermissao("Papeis.GerenciarPermissoes"))
        {
            return StatusCode(403, new RespostaErro 
            { 
                Erro = "Acesso negado", 
                Mensagem = "Acesso negado para comparar papéis" 
            });
        }

        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            _logger.LogInformation("🔍 Usuario {UsuarioId} comparando papéis: {PapeisIds}",
                usuarioLogadoId, string.Join(", ", papeisIds));

            // ✅ VALIDAR ENTRADA
            if (papeisIds == null || papeisIds.Count < 2)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "Dados insuficientes",
                    Mensagem = "É necessário pelo menos 2 papéis para comparação"
                });
            }

            if (papeisIds.Count > 10)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "Muitos papéis",
                    Mensagem = "Máximo de 10 papéis para comparação simultânea"
                });
            }

            // ✅ VERIFICAR SE PAPÉIS EXISTEM
            var papeis = await _context.Papeis
                .Where(p => papeisIds.Contains(p.Id))
                .ToListAsync();

            var papeisNaoEncontrados = papeisIds.Except(papeis.Select(p => p.Id)).ToList();
            if (papeisNaoEncontrados.Any())
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "Papéis não encontrados",
                    Mensagem = $"Papéis não encontrados: {string.Join(", ", papeisNaoEncontrados)}"
                });
            }

            // ✅ OBTER PERMISSÕES DE CADA PAPEL
            var permissoesPorPapel = new Dictionary<int, List<PermissaoDetalhada>>();

            foreach (var papel in papeis)
            {
                var permissoes = await _context.PapelPermissoes
                    .Include(pp => pp.Permissao)
                    .Where(pp => pp.PapelId == papel.Id && pp.Ativo)
                    .Select(pp => new PermissaoDetalhada
                    {
                        Id = pp.Permissao.Id,
                        Nome = pp.Permissao.Nome,
                        Categoria = pp.Permissao.Categoria,
                        DataAtribuicao = pp.DataAtribuicao,
                        Ativo = pp.Ativo
                    })
                    .ToListAsync();

                permissoesPorPapel[papel.Id] = permissoes;
            }

            // ✅ ANÁLISE DE COMPARAÇÃO
            var todasPermissoes = permissoesPorPapel.Values
                .SelectMany(p => p)
                .GroupBy(p => p.Id)
                .ToList();

            var permissoesComuns = todasPermissoes
                .Where(g => g.Count() == papeis.Count)
                .Select(g => g.First())
                .ToList();

            var permissoesExclusivas = new Dictionary<int, List<PermissaoDetalhada>>();
            foreach (var papel in papeis)
            {
                var exclusivas = permissoesPorPapel[papel.Id]
                    .Where(p => !permissoesComuns.Any(pc => pc.Id == p.Id))
                    .ToList();
                permissoesExclusivas[papel.Id] = exclusivas;
            }

            var permissoesParciais = todasPermissoes
                .Where(g => g.Count() > 1 && g.Count() < papeis.Count)
                .Select(g => new PermissaoParcial
                {
                    Permissao = g.First(),
                    PapeisQuetem = g.Select(p => papeis.First(papel =>
                        permissoesPorPapel[papel.Id].Any(perm => perm.Id == p.Id)).Name ?? "N/A").ToList()
                })
                .ToList();

            // ✅ CONSTRUIR RESULTADO
            var comparacao = new ComparacaoMultiplosPapeis
            {
                PapeisComparados = papeis.Select(p => new PapelResumoRelatorio
                {
                    Id = p.Id,
                    Nome = p.Name ?? "N/A",
                    Descricao = p.Descricao,
                    Categoria = p.Categoria,
                    Nivel = p.Nivel,
                    Ativo = p.Ativo
                }).ToList(),
                PermissoesComuns = mostrarApenasConflitos ? new() : permissoesComuns,
                PermissoesExclusivas = permissoesExclusivas,
                PermissoesParciais = permissoesParciais,
                Estatisticas = new EstatisticasComparacao
                {
                    TotalPermissoesComuns = permissoesComuns.Count,
                    TotalPermissoesExclusivas = permissoesExclusivas.Values.Sum(list => list.Count),
                    TotalPermissoesParciais = permissoesParciais.Count,
                    PapelComMaisPermissoes = papeis.OrderByDescending(p => permissoesPorPapel[p.Id].Count).First().Name ?? "N/A",
                    PapelComMenosPermissoes = papeis.OrderBy(p => permissoesPorPapel[p.Id].Count).First().Name ?? "N/A"
                },
                DataComparacao = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Comparação concluída: {Comuns} comuns, {Exclusivas} exclusivas, {Parciais} parciais",
                permissoesComuns.Count,
                permissoesExclusivas.Values.Sum(list => list.Count),
                permissoesParciais.Count);

            return Ok(comparacao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao comparar papéis");
            return StatusCode(500, new RespostaErro
            {
                Erro = "Erro interno",
                Mensagem = "Erro ao comparar papéis"
            });
        }
    }

    /// <summary>
    /// Identifica permissões órfãs e dados inconsistentes no sistema
    /// </summary>
    /// <param name="executarLimpeza">Se true, executa limpeza automática dos dados órfãos</param>
    /// <returns>Relatório de permissões órfãs e inconsistências</returns>
    [HttpGet("orphans")]
    [ProducesResponseType(typeof(RelatorioPermissoesOrfas), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> IdentificarPermissoesOrfas([FromQuery] bool executarLimpeza = false)
    {
        if (!TemPermissao("Sistema.Controle.Total"))
        {
            return StatusCode(403, new RespostaErro 
            { 
                Erro = "Acesso negado", 
                Mensagem = "Acesso negado para identificar permissões órfãs - requer controle total do sistema" 
            });
        }

        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            _logger.LogInformation("🧹 Usuario {UsuarioId} identificando permissões órfãs", usuarioLogadoId);

            // ✅ IDENTIFICAR ASSOCIAÇÕES COM PAPÉIS INATIVOS
            var associacoesComPapeisInativos = await _context.PapelPermissoes
                .Include(pp => pp.Papel)
                .Include(pp => pp.Permissao)
                .Where(pp => pp.Ativo && !pp.Papel.Ativo)
                .Select(pp => new AssociacaoOrfa
                {
                    PapelId = pp.PapelId,
                    PapelNome = pp.Papel.Name ?? "N/A",
                    PermissaoId = pp.PermissaoId,
                    PermissaoNome = pp.Permissao.Nome,
                    TipoProblema = "Papel Inativo",
                    DataAtribuicao = pp.DataAtribuicao
                })
                .ToListAsync();

            // ✅ IDENTIFICAR ASSOCIAÇÕES COM PERMISSÕES INATIVAS
            var associacoesComPermissoesInativas = await _context.PapelPermissoes
                .Include(pp => pp.Papel)
                .Include(pp => pp.Permissao)
                .Where(pp => pp.Ativo && !pp.Permissao.Ativo)
                .Select(pp => new AssociacaoOrfa
                {
                    PapelId = pp.PapelId,
                    PapelNome = pp.Papel.Name ?? "N/A",
                    PermissaoId = pp.PermissaoId,
                    PermissaoNome = pp.Permissao.Nome,
                    TipoProblema = "Permissão Inativa",
                    DataAtribuicao = pp.DataAtribuicao
                })
                .ToListAsync();

            // ✅ IDENTIFICAR PERMISSÕES NUNCA ATRIBUÍDAS
            var permissoesNuncaAtribuidas = await _context.Permissoes
                .Where(p => p.Ativo && !_context.PapelPermissoes.Any(pp => pp.PermissaoId == p.Id && pp.Ativo))
                .Select(p => new PermissaoNaoUtilizada
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Descricao = p.Descricao,
                    Categoria = p.Categoria,
                    DataCriacao = p.DataCriacao,
                    DiasDesdeUltimoUso = (DateTime.UtcNow - p.DataCriacao).Days
                })
                .ToListAsync();

            // ✅ IDENTIFICAR PAPÉIS SEM PERMISSÕES
            var papeisSemPermissoes = await _context.Papeis
                .Where(p => p.Ativo && !_context.PapelPermissoes.Any(pp => pp.PapelId == p.Id && pp.Ativo))
                .Select(p => new PapelSemPermissoes
                {
                    Id = p.Id,
                    Nome = p.Name ?? "N/A",
                    Descricao = p.Descricao,
                    Categoria = p.Categoria,
                    DataCriacao = p.DataCriacao,
                    TotalUsuarios = _context.UsuarioPapeis.Count(up => up.PapelId == p.Id && up.Ativo)
                })
                .ToListAsync();

            var relatorio = new RelatorioPermissoesOrfas
            {
                AssociacoesComPapeisInativos = associacoesComPapeisInativos,
                AssociacoesComPermissoesInativas = associacoesComPermissoesInativas,
                PermissoesNuncaAtribuidas = permissoesNuncaAtribuidas,
                PapeisSemPermissoes = papeisSemPermissoes,
                Resumo = new ResumoLimpeza
                {
                    TotalAssociacoesComPapeisInativos = associacoesComPapeisInativos.Count,
                    TotalAssociacoesComPermissoesInativas = associacoesComPermissoesInativas.Count,
                    TotalPermissoesNaoUtilizadas = permissoesNuncaAtribuidas.Count,
                    TotalPapeisSemPermissoes = papeisSemPermissoes.Count,
                    LimpezaExecutada = false,
                    DataAnalise = DateTime.UtcNow
                }
            };

            // ✅ EXECUTAR LIMPEZA SE SOLICITADO
            if (executarLimpeza && (associacoesComPapeisInativos.Any() || associacoesComPermissoesInativas.Any()))
            {
                var resultadoLimpeza = await ExecutarLimpezaOrfas(associacoesComPapeisInativos, associacoesComPermissoesInativas);
                relatorio.Resumo.LimpezaExecutada = true;
                relatorio.Resumo.ItensLimpos = resultadoLimpeza.ItensLimpos;
                relatorio.Resumo.ErrosLimpeza = resultadoLimpeza.Erros;
            }

            _logger.LogInformation("✅ Análise de órfãs concluída: {Papeis} papéis inativos, {Permissoes} permissões inativas, {NaoUtilizadas} não utilizadas",
                associacoesComPapeisInativos.Count,
                associacoesComPermissoesInativas.Count,
                permissoesNuncaAtribuidas.Count);

            return Ok(relatorio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao identificar permissões órfãs");
            return StatusCode(500, new RespostaErro
            {
                Erro = "Erro interno",
                Mensagem = "Erro ao identificar permissões órfãs"
            });
        }
    }

    /// <summary>
    /// Gera estatísticas detalhadas do sistema de permissões
    /// </summary>
    /// <param name="periodo">Período para análise: ultimos7dias, ultimos30dias, ultimos90dias, todos</param>
    /// <returns>Estatísticas detalhadas do sistema</returns>
    [HttpGet("estatisticas")]
    [ProducesResponseType(typeof(EstatisticasDetalhadas), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterEstatisticasDetalhadas([FromQuery] string periodo = "todos")
    {
        if (!TemPermissao("Papeis.GerenciarPermissoes") && !TemPermissao("Sistema.Controle.Total"))
        {
            return StatusCode(403, new RespostaErro 
            { 
                Erro = "Acesso negado", 
                Mensagem = "Acesso negado para visualizar estatísticas detalhadas" 
            });
        }

        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            _logger.LogInformation("📈 Usuario {UsuarioId} obtendo estatísticas detalhadas - período: {Periodo}",
                usuarioLogadoId, periodo);

            // ✅ CALCULAR DATA DE INÍCIO BASEADA NO PERÍODO
            var dataInicio = CalcularDataInicioPeriodo(periodo);

            // ✅ ESTATÍSTICAS GERAIS
            var estatisticasGerais = await GerarEstatisticasGerais(dataInicio);

            // ✅ TENDÊNCIAS TEMPORAIS
            var tendencias = await GerarTendenciasTempo(dataInicio);

            // ✅ DISTRIBUIÇÕES
            var distribuicoes = await GerarDistribuicoes();

            // ✅ TOP RANKINGS
            var rankings = await GerarRankings();

            var estatisticas = new EstatisticasDetalhadas
            {
                Periodo = periodo,
                DataInicio = dataInicio,
                DataFim = DateTime.UtcNow,
                EstatisticasGerais = estatisticasGerais,
                Tendencias = tendencias,
                Distribuicoes = distribuicoes,
                Rankings = rankings,
                DataGeracao = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Estatísticas detalhadas geradas para período: {Periodo}", periodo);

            return Ok(estatisticas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao gerar estatísticas detalhadas");
            return StatusCode(500, new RespostaErro
            {
                Erro = "Erro interno",
                Mensagem = "Erro ao gerar estatísticas detalhadas"
            });
        }
    }

    #region Métodos Auxiliares

    /// <summary>
    /// Aplica filtros à query de associações papel-permissão
    /// </summary>
    private IQueryable<PapelPermissao> AplicarFiltros(IQueryable<PapelPermissao> query, FiltrosAssociacoes filtros)
    {
        // ✅ FILTRO POR IDS DE PAPÉIS
        if (filtros.PapeisIds?.Any() == true)
        {
            query = query.Where(pp => filtros.PapeisIds.Contains(pp.PapelId));
        }

        // ✅ FILTRO POR IDS DE PERMISSÕES
        if (filtros.PermissoesIds?.Any() == true)
        {
            query = query.Where(pp => filtros.PermissoesIds.Contains(pp.PermissaoId));
        }

        // ✅ FILTRO POR CATEGORIA DO PAPEL
        if (!string.IsNullOrEmpty(filtros.CategoriaPapel))
        {
            query = query.Where(pp => pp.Papel.Categoria != null &&
                pp.Papel.Categoria.ToLower().Contains(filtros.CategoriaPapel.ToLower()));
        }

        // ✅ FILTRO POR CATEGORIA DA PERMISSÃO
        if (!string.IsNullOrEmpty(filtros.CategoriaPermissao))
        {
            query = query.Where(pp => pp.Permissao.Categoria != null &&
                pp.Permissao.Categoria.ToLower().Contains(filtros.CategoriaPermissao.ToLower()));
        }

        // ✅ FILTRO POR STATUS ATIVO/INATIVO
        if (filtros.Ativo.HasValue)
        {
            query = query.Where(pp => pp.Ativo == filtros.Ativo.Value);
        }

        // ✅ FILTRO POR PERÍODO DE ATRIBUIÇÃO
        if (filtros.DataAtribuicaoInicio.HasValue)
        {
            query = query.Where(pp => pp.DataAtribuicao >= filtros.DataAtribuicaoInicio.Value);
        }

        if (filtros.DataAtribuicaoFim.HasValue)
        {
            query = query.Where(pp => pp.DataAtribuicao <= filtros.DataAtribuicaoFim.Value);
        }

        // ✅ FILTRO POR ASSOCIAÇÕES ÓRFÃS
        if (filtros.ApenasAssociacoesOrfas)
        {
            query = query.Where(pp => !pp.Papel.Ativo || !pp.Permissao.Ativo);
        }

        // ✅ INCLUIR INATIVOS (da classe base)
        if (!filtros.IncluirInativos)
        {
            query = query.Where(pp => pp.Ativo && pp.Papel.Ativo && pp.Permissao.Ativo);
        }

        return query;
    }

    /// <summary>
    /// Aplica ordenação à query de associações
    /// </summary>
    private IQueryable<PapelPermissao> AplicarOrdenacao(IQueryable<PapelPermissao> query, string? ordenarPor, string? direcao)
    {
        var isDesc = !string.IsNullOrEmpty(direcao) && direcao.ToLower() == "desc";

        return ordenarPor?.ToLower() switch
        {
            "papel" => isDesc ? query.OrderByDescending(pp => pp.Papel.Name)
                              : query.OrderBy(pp => pp.Papel.Name),
            "permissao" => isDesc ? query.OrderByDescending(pp => pp.Permissao.Nome)
                                  : query.OrderBy(pp => pp.Permissao.Nome),
            "dataatribuicao" => isDesc ? query.OrderByDescending(pp => pp.DataAtribuicao)
                                       : query.OrderBy(pp => pp.DataAtribuicao),
            "ativo" => isDesc ? query.OrderByDescending(pp => pp.Ativo)
                              : query.OrderBy(pp => pp.Ativo),
            "categoriapapel" => isDesc ? query.OrderByDescending(pp => pp.Papel.Categoria)
                                       : query.OrderBy(pp => pp.Papel.Categoria),
            "categoriapermissao" => isDesc ? query.OrderByDescending(pp => pp.Permissao.Categoria)
                                           : query.OrderBy(pp => pp.Permissao.Categoria),
            _ => query.OrderBy(pp => pp.Papel.Name).ThenBy(pp => pp.Permissao.Nome) // Ordenação padrão
        };
    }

    /// <summary>
    /// Gera dados de comparação entre papéis
    /// </summary>
    private async Task<ComparacaoPapeis> GerarComparacaoPapeis(int papelId)
    {
        // ✅ ESTATÍSTICAS GERAIS DE PERMISSÕES POR PAPEL
        var estatisticasPapeis = await _context.PapelPermissoes
            .Where(pp => pp.Ativo && pp.Papel.Ativo)
            .GroupBy(pp => new { pp.PapelId, pp.Papel.Name })
            .Select(g => new { PapelId = g.Key.PapelId, PapelNome = g.Key.Name, TotalPermissoes = g.Count() })
            .OrderByDescending(x => x.TotalPermissoes)
            .ToListAsync();

        if (!estatisticasPapeis.Any())
        {
            return new ComparacaoPapeis();
        }

        var papelAtual = estatisticasPapeis.FirstOrDefault(x => x.PapelId == papelId);
        var posicaoAtual = estatisticasPapeis.FindIndex(x => x.PapelId == papelId) + 1;

        return new ComparacaoPapeis
        {
            PapelComMaisPermissoes = estatisticasPapeis.First().PapelNome,
            PapelComMenosPermissoes = estatisticasPapeis.Last().PapelNome,
            PosicaoRanking = posicaoAtual,
            MediaPermissoesPorPapel = (decimal)estatisticasPapeis.Average(x => x.TotalPermissoes)
        };
    }

    /// <summary>
    /// Verifica se o usuário logado tem uma permissão específica
    /// </summary>
    private bool TemPermissao(string permissao)
    {
        try
        {
            // ✅ MÉTODO 1: Verificar claims de permissão
            var permissaoClaim = User.FindAll("permissao")
                .Any(c => c.Value == permissao);

            if (permissaoClaim)
            {
                _logger.LogInformation("✅ Permissão '{Permissao}' encontrada via claim", permissao);
                return true;
            }

            // ✅ MÉTODO 2: Verificar se é SuperAdmin (controle total)
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            if (isSuperAdmin)
            {
                _logger.LogInformation("✅ Usuário é SuperAdmin - acesso liberado para '{Permissao}'", permissao);
                return true;
            }

            // ✅ MÉTODO 3: Verificar controle total do sistema
            var hasSystemControl = User.FindAll("permissao")
                .Any(c => c.Value == "Sistema.Controle.Total");

            if (hasSystemControl)
            {
                _logger.LogInformation("✅ Usuário tem Sistema.Controle.Total - acesso liberado para '{Permissao}'", permissao);
                return true;
            }

            _logger.LogWarning("❌ Permissão '{Permissao}' negada. Usuário: {Usuario}, Papéis: [{Papeis}], Permissões: [{Permissoes}]", 
                permissao,
                User.Identity?.Name ?? "Desconhecido",
                string.Join(", ", User.FindAll("role").Select(c => c.Value)),
                string.Join(", ", User.FindAll("permissao").Select(c => c.Value).Take(5))
            );

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao verificar permissão '{Permissao}'", permissao);
            return false;
        }
    }

    /// <summary>
    /// Obtém ID do usuário logado
    /// </summary>
    private int ObterUsuarioLogadoId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("user_id") ?? User.FindFirst("id");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }

        _logger.LogWarning("⚠️ Não foi possível obter ID do usuário logado");
        return 0;
    }

    #endregion

    #region Métodos Auxiliares de Operações em Lote

    /// <summary>
    /// Executa associação em lote
    /// </summary>
    private async Task<RespostaAssociacaoLote> ExecutarAssociacaoEmLote(List<Papel> papeis, List<Permissao> permissoes)
    {
        var detalhes = new List<DetalheOperacao>();
        int sucessos = 0;

        foreach (var papel in papeis)
        {
            foreach (var permissao in permissoes)
            {
                try
                {
                    // Verificar se associação já existe
                    var existente = await _context.PapelPermissoes
                        .FirstOrDefaultAsync(pp => pp.PapelId == papel.Id && pp.PermissaoId == permissao.Id);

                    if (existente != null && existente.Ativo)
                    {
                        detalhes.Add(new DetalheOperacao
                        {
                            PapelId = papel.Id,
                            PermissaoId = permissao.Id,
                            Sucesso = false,
                            Erro = "Associação já existe"
                        });
                        continue;
                    }

                    if (existente != null && !existente.Ativo)
                    {
                        // Reativar associação existente
                        existente.Ativo = true;
                        existente.DataAtribuicao = DateTime.UtcNow;
                    }
                    else
                    {
                        // Criar nova associação
                        _context.PapelPermissoes.Add(new PapelPermissao
                        {
                            PapelId = papel.Id,
                            PermissaoId = permissao.Id,
                            DataAtribuicao = DateTime.UtcNow,
                            Ativo = true
                        });
                    }

                    detalhes.Add(new DetalheOperacao
                    {
                        PapelId = papel.Id,
                        PermissaoId = permissao.Id,
                        Sucesso = true
                    });
                    sucessos++;
                }
                catch (Exception ex)
                {
                    detalhes.Add(new DetalheOperacao
                    {
                        PapelId = papel.Id,
                        PermissaoId = permissao.Id,
                        Sucesso = false,
                        Erro = ex.Message
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        return new RespostaAssociacaoLote
        {
            Sucesso = sucessos > 0,
            TotalProcessadas = detalhes.Count,
            TotalSucessos = sucessos,
            TotalFalhas = detalhes.Count - sucessos,
            Detalhes = detalhes
        };
    }

    /// <summary>
    /// Executa dissociação em lote
    /// </summary>
    private async Task<RespostaAssociacaoLote> ExecutarDissociacaoEmLote(List<Papel> papeis, List<Permissao> permissoes)
    {
        var detalhes = new List<DetalheOperacao>();
        int sucessos = 0;

        foreach (var papel in papeis)
        {
            foreach (var permissao in permissoes)
            {
                try
                {
                    var associacao = await _context.PapelPermissoes
                        .FirstOrDefaultAsync(pp => pp.PapelId == papel.Id && pp.PermissaoId == permissao.Id && pp.Ativo);

                    if (associacao == null)
                    {
                        detalhes.Add(new DetalheOperacao
                        {
                            PapelId = papel.Id,
                            PermissaoId = permissao.Id,
                            Sucesso = false,
                            Erro = "Associação não encontrada"
                        });
                        continue;
                    }

                    associacao.Ativo = false;
                    detalhes.Add(new DetalheOperacao
                    {
                        PapelId = papel.Id,
                        PermissaoId = permissao.Id,
                        Sucesso = true
                    });
                    sucessos++;
                }
                catch (Exception ex)
                {
                    detalhes.Add(new DetalheOperacao
                    {
                        PapelId = papel.Id,
                        PermissaoId = permissao.Id,
                        Sucesso = false,
                        Erro = ex.Message
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        return new RespostaAssociacaoLote
        {
            Sucesso = sucessos > 0,
            TotalProcessadas = detalhes.Count,
            TotalSucessos = sucessos,
            TotalFalhas = detalhes.Count - sucessos,
            Detalhes = detalhes
        };
    }

    /// <summary>
    /// Executa substituição em lote
    /// </summary>
    private async Task<RespostaAssociacaoLote> ExecutarSubstituicaoEmLote(List<Papel> papeis, List<Permissao> permissoes)
    {
        var detalhes = new List<DetalheOperacao>();
        int sucessos = 0;

        foreach (var papel in papeis)
        {
            try
            {
                // Desativar todas as associações atuais do papel
                var associacoesAtuais = await _context.PapelPermissoes
                    .Where(pp => pp.PapelId == papel.Id && pp.Ativo)
                    .ToListAsync();

                foreach (var associacao in associacoesAtuais)
                {
                    associacao.Ativo = false;
                }

                // Criar/reativar associações com as novas permissões
                foreach (var permissao in permissoes)
                {
                    var associacaoExistente = await _context.PapelPermissoes
                        .FirstOrDefaultAsync(pp => pp.PapelId == papel.Id && pp.PermissaoId == permissao.Id);

                    if (associacaoExistente != null)
                    {
                        associacaoExistente.Ativo = true;
                        associacaoExistente.DataAtribuicao = DateTime.UtcNow;
                    }
                    else
                    {
                        _context.PapelPermissoes.Add(new PapelPermissao
                        {
                            PapelId = papel.Id,
                            PermissaoId = permissao.Id,
                            DataAtribuicao = DateTime.UtcNow,
                            Ativo = true
                        });
                    }

                    detalhes.Add(new DetalheOperacao
                    {
                        PapelId = papel.Id,
                        PermissaoId = permissao.Id,
                        Sucesso = true
                    });
                    sucessos++;
                }
            }
            catch (Exception ex)
            {
                foreach (var permissao in permissoes)
                {
                    detalhes.Add(new DetalheOperacao
                    {
                        PapelId = papel.Id,
                        PermissaoId = permissao.Id,
                        Sucesso = false,
                        Erro = ex.Message
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        return new RespostaAssociacaoLote
        {
            Sucesso = sucessos > 0,
            TotalProcessadas = detalhes.Count,
            TotalSucessos = sucessos,
            TotalFalhas = detalhes.Count - sucessos,
            Detalhes = detalhes
        };
    }

    #endregion

    #region Métodos Auxiliares de Análise Avançada

    /// <summary>
    /// Gera estatísticas avançadas do sistema
    /// </summary>
    private async Task<EstatisticasAvancadas> GerarEstatisticasAvancadas()
    {
        // Complexidade média de papéis (número médio de permissões por papel)
        var complexidadeMedia = await _context.PapelPermissoes
            .Where(pp => pp.Ativo)
            .GroupBy(pp => pp.PapelId)
            .Select(g => g.Count())
            .DefaultIfEmpty(0)
            .AverageAsync();

        // Cobertura de permissões (percentual de permissões em uso)
        var totalPermissoes = await _context.Permissoes.CountAsync(p => p.Ativo);
        var permissoesEmUso = await _context.PapelPermissoes
            .Where(pp => pp.Ativo)
            .Select(pp => pp.PermissaoId)
            .Distinct()
            .CountAsync();

        var coberturaPermissoes = totalPermissoes > 0 ? (decimal)permissoesEmUso / totalPermissoes * 100 : 0;

        // Redundância de papéis (permissões que aparecem em muitos papéis)
        var redundanciaMedia = await _context.PapelPermissoes
            .Where(pp => pp.Ativo)
            .GroupBy(pp => pp.PermissaoId)
            .Select(g => g.Count())
            .DefaultIfEmpty(0)
            .AverageAsync();

        return new EstatisticasAvancadas
        {
            ComplexidadeMediaPapeis = Math.Round(complexidadeMedia, 2),
            CoberturaPermissoes = Math.Round(coberturaPermissoes, 2),
            RedundanciaMediaPermissoes = Math.Round(redundanciaMedia, 2),
            TaxaUtilizacaoSistema = Math.Round(coberturaPermissoes * (decimal)redundanciaMedia / 100, 2)
        };
    }

    /// <summary>
    /// Gera comparações do sistema
    /// </summary>
    private async Task<ComparacoesSistema> GerarComparacoesSistema()
    {
        var papeisMaisComplexos = await _context.PapelPermissoes
            .Include(pp => pp.Papel)
            .Where(pp => pp.Ativo && pp.Papel.Ativo)
            .GroupBy(pp => new { pp.PapelId, pp.Papel.Name })
            .Select(g => new { PapelId = g.Key.PapelId, Nome = g.Key.Name, TotalPermissoes = g.Count() })
            .OrderByDescending(x => x.TotalPermissoes)
            .Take(5)
            .ToListAsync();

        var permissoesMaisUsadas = await _context.PapelPermissoes
            .Include(pp => pp.Permissao)
            .Where(pp => pp.Ativo && pp.Permissao.Ativo)
            .GroupBy(pp => new { pp.PermissaoId, pp.Permissao.Nome })
            .Select(g => new { PermissaoId = g.Key.PermissaoId, Nome = g.Key.Nome, TotalPapeis = g.Count() })
            .OrderByDescending(x => x.TotalPapeis)
            .Take(5)
            .ToListAsync();

        return new ComparacoesSistema
        {
            PapeisMaisComplexos = papeisMaisComplexos.Select(p => $"{p.Nome} ({p.TotalPermissoes} permissões)").ToList(),
            PermissoesMaisUsadas = permissoesMaisUsadas.Select(p => $"{p.Nome} ({p.TotalPapeis} papéis)").ToList()
        };
    }

    /// <summary>
    /// Executa limpeza de dados órfãos
    /// </summary>
    private async Task<ResultadoLimpeza> ExecutarLimpezaOrfas(
        List<AssociacaoOrfa> associacoesComPapeisInativos, 
        List<AssociacaoOrfa> associacoesComPermissoesInativas)
    {
        var itensLimpos = 0;
        var erros = new List<string>();

        try
        {
            // Desativar associações com papéis inativos
            foreach (var associacao in associacoesComPapeisInativos)
            {
                var pp = await _context.PapelPermissoes
                    .FirstOrDefaultAsync(x => x.PapelId == associacao.PapelId && x.PermissaoId == associacao.PermissaoId);
                
                if (pp != null && pp.Ativo)
                {
                    pp.Ativo = false;
                    itensLimpos++;
                }
            }

            // Desativar associações com permissões inativas
            foreach (var associacao in associacoesComPermissoesInativas)
            {
                var pp = await _context.PapelPermissoes
                    .FirstOrDefaultAsync(x => x.PapelId == associacao.PapelId && x.PermissaoId == associacao.PermissaoId);
                
                if (pp != null && pp.Ativo)
                {
                    pp.Ativo = false;
                    itensLimpos++;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("🧹 Limpeza executada: {ItensLimpos} itens limpos", itensLimpos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro durante limpeza de órfãs");
            erros.Add($"Erro durante limpeza: {ex.Message}");
        }

        return new ResultadoLimpeza { ItensLimpos = itensLimpos, Erros = erros };
    }

    /// <summary>
    /// Calcula data de início baseada no período solicitado
    /// </summary>
    private DateTime? CalcularDataInicioPeriodo(string periodo)
    {
        return periodo.ToLower() switch
        {
            "ultimos7dias" => DateTime.UtcNow.AddDays(-7),
            "ultimos30dias" => DateTime.UtcNow.AddDays(-30),
            "ultimos90dias" => DateTime.UtcNow.AddDays(-90),
            "todos" => null,
            _ => null
        };
    }

    /// <summary>
    /// Gera estatísticas gerais do período
    /// </summary>
    private async Task<EstatisticasGeraisPeriodo> GerarEstatisticasGerais(DateTime? dataInicio)
    {
        var query = _context.PapelPermissoes.AsQueryable();
        
        if (dataInicio.HasValue)
        {
            query = query.Where(pp => pp.DataAtribuicao >= dataInicio.Value);
        }

        var totalNovasAssociacoes = await query.CountAsync();
        var associacoesAtivas = await query.CountAsync(pp => pp.Ativo);

        return new EstatisticasGeraisPeriodo
        {
            NovasAssociacoes = totalNovasAssociacoes,
            AssociacoesAtivas = associacoesAtivas,
            TaxaCrescimento = dataInicio.HasValue ? await CalcularTaxaCrescimento(dataInicio.Value) : 0
        };
    }

    /// <summary>
    /// Gera tendências temporais
    /// </summary>
    private async Task<TendenciasTempo> GerarTendenciasTempo(DateTime? dataInicio)
    {
        if (!dataInicio.HasValue)
        {
            return new TendenciasTempo();
        }

        var associacoesPorDia = await _context.PapelPermissoes
            .Where(pp => pp.DataAtribuicao >= dataInicio.Value)
            .GroupBy(pp => pp.DataAtribuicao.Date)
            .Select(g => new { Data = g.Key, Total = g.Count() })
            .OrderBy(x => x.Data)
            .ToListAsync();

        return new TendenciasTempo
        {
            AssociacoesPorDia = associacoesPorDia.ToDictionary(x => x.Data, x => x.Total)
        };
    }

    /// <summary>
    /// Gera distribuições
    /// </summary>
    private async Task<Distribuicoes> GerarDistribuicoes()
    {
        var porCategoria = await _context.PapelPermissoes
            .Include(pp => pp.Permissao)
            .Where(pp => pp.Ativo)
            .GroupBy(pp => pp.Permissao.Categoria ?? "Sem Categoria")
            .Select(g => new { Categoria = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Categoria, x => x.Total);

        var porNivelPapel = await _context.PapelPermissoes
            .Include(pp => pp.Papel)
            .Where(pp => pp.Ativo)
            .GroupBy(pp => pp.Papel.Nivel)
            .Select(g => new { Nivel = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Nivel, x => x.Total);

        return new Distribuicoes
        {
            PorCategoria = porCategoria,
            PorNivelPapel = porNivelPapel
        };
    }

    /// <summary>
    /// Gera rankings
    /// </summary>
    private async Task<Rankings> GerarRankings()
    {
        var topPapeis = await _context.PapelPermissoes
            .Include(pp => pp.Papel)
            .Where(pp => pp.Ativo)
            .GroupBy(pp => new { pp.PapelId, pp.Papel.Name })
            .Select(g => new { Nome = g.Key.Name ?? "N/A", Total = g.Count() })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToDictionaryAsync(x => x.Nome, x => x.Total);

        var topPermissoes = await _context.PapelPermissoes
            .Include(pp => pp.Permissao)
            .Where(pp => pp.Ativo)
            .GroupBy(pp => new { pp.PermissaoId, pp.Permissao.Nome })
            .Select(g => new { Nome = g.Key.Nome, Total = g.Count() })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToDictionaryAsync(x => x.Nome, x => x.Total);

        return new Rankings
        {
            TopPapeisPorPermissoes = topPapeis,
            TopPermissoesPorPapeis = topPermissoes
        };
    }

    /// <summary>
    /// Calcula taxa de crescimento
    /// </summary>
    private async Task<decimal> CalcularTaxaCrescimento(DateTime dataInicio)
    {
        var periodoAnterior = dataInicio.AddDays(-(DateTime.UtcNow - dataInicio).Days);
        
        var associacoesPeriodoAtual = await _context.PapelPermissoes
            .CountAsync(pp => pp.DataAtribuicao >= dataInicio);
            
        var associacoesPeriodoAnterior = await _context.PapelPermissoes
            .CountAsync(pp => pp.DataAtribuicao >= periodoAnterior && pp.DataAtribuicao < dataInicio);

        if (associacoesPeriodoAnterior == 0) return 0;

        return Math.Round(((decimal)associacoesPeriodoAtual - associacoesPeriodoAnterior) / associacoesPeriodoAnterior * 100, 2);
    }

    #endregion
}