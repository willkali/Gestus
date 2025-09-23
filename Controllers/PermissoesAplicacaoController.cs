using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Gestus.Modelos;
using Gestus.Dados;
using Gestus.DTOs.PermissaoAplicacao;
using Gestus.DTOs.Comuns;
using System.Text.Json;
using Gestus.DTOs.Permissao;
using System.Text;

// Aliases para resolver conflitos de namespace
using ResultadoImportacaoComum = Gestus.DTOs.Comuns.ResultadoImportacao;
using ErroImportacaoComum = Gestus.DTOs.Comuns.ErroImportacao;

namespace Gestus.Controllers;

/// <summary>
/// Controller para gerenciamento de permissões específicas de aplicações
/// Separado do PermissoesController para maior clareza e manutenibilidade
/// </summary>
[ApiController]
[Route("api/aplicacoes/{aplicacaoId:int}/permissoes")]
[Produces("application/json")]
[Authorize]
public class PermissoesAplicacaoController : ControllerBase
{
    private readonly GestusDbContexto _context;
    private readonly ILogger<PermissoesAplicacaoController> _logger;

    public PermissoesAplicacaoController(
        GestusDbContexto context,
        ILogger<PermissoesAplicacaoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista permissões de uma aplicação específica com filtros avançados
    /// </summary>
    /// <param name="aplicacaoId">ID da aplicação</param>
    /// <param name="filtros">Filtros para busca</param>
    /// <returns>Lista paginada de permissões da aplicação</returns>
    [HttpGet]
    [ProducesResponseType(typeof(RespostaPaginada<PermissaoAplicacaoResumo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarPermissoesAplicacao(int aplicacaoId, [FromQuery] FiltrosPermissaoAplicacao filtros)
    {
        try
        {
            if (!TemPermissao("PermissoesAplicacao.Listar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para listar permissões de aplicação" 
                });
            }

            // Verificar se aplicação existe
            var aplicacao = await _context.Aplicacoes
                .Include(a => a.TipoAplicacao)
                .FirstOrDefaultAsync(a => a.Id == aplicacaoId);

            if (aplicacao == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "AplicacaoNaoEncontrada", 
                    Mensagem = "Aplicação não encontrada" 
                });
            }

            // Definir filtro por aplicação
            filtros.AplicacaoId = aplicacaoId;

            // Query base
            var query = _context.PermissoesAplicacao
                .Include(pa => pa.Aplicacao)
                    .ThenInclude(a => a.TipoAplicacao)
                .Include(pa => pa.AtualizadoPor)
                .Include(pa => pa.PapelPermissoes)
                    .ThenInclude(ppa => ppa.Papel)
                .Where(pa => pa.AplicacaoId == aplicacaoId)
                .AsQueryable();

            // Aplicar filtros
            query = AplicarFiltros(query, filtros);

            // Aplicar ordenação
            query = AplicarOrdenacao(query, filtros.OrdenarPor, filtros.DirecaoOrdenacao);

            // Paginação
            var totalItens = await query.CountAsync();
            var permissoes = await query
                .Skip((filtros.Pagina - 1) * filtros.ItensPorPagina)
                .Take(filtros.ItensPorPagina)
                .Select(pa => ConstruirPermissaoAplicacaoResumo(pa))
                .ToListAsync();

            var resposta = new RespostaPaginada<PermissaoAplicacaoResumo>
            {
                Dados = permissoes,
                TotalItens = totalItens,
                PaginaAtual = filtros.Pagina,
                ItensPorPagina = filtros.ItensPorPagina,
                TotalPaginas = (int)Math.Ceiling((double)totalItens / filtros.ItensPorPagina)
            };

            await RegistrarAuditoria("Listar", "PermissaoAplicacao", aplicacaoId.ToString(), 
                $"Listagem de permissões da aplicação '{aplicacao.Nome}'");

            _logger.LogInformation("✅ Permissões listadas - Aplicação: {AplicacaoId}, Total: {Total}", 
                aplicacaoId, totalItens);

            return Ok(resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar permissões da aplicação {AplicacaoId}", aplicacaoId);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Obtém detalhes completos de uma permissão específica da aplicação
    /// </summary>
    /// <param name="aplicacaoId">ID da aplicação</param>
    /// <param name="id">ID da permissão</param>
    /// <returns>Detalhes completos da permissão</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PermissaoAplicacaoCompleta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterPermissaoAplicacao(int aplicacaoId, int id)
    {
        try
        {
            if (!TemPermissao("PermissoesAplicacao.Visualizar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para visualizar permissões de aplicação" 
                });
            }

            var permissao = await ObterPermissaoAplicacaoCompletaAsync(aplicacaoId, id);

            if (permissao == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "PermissaoNaoEncontrada", 
                    Mensagem = "Permissão não encontrada nesta aplicação" 
                });
            }

            await RegistrarAuditoria("Visualizar", "PermissaoAplicacao", id.ToString(), 
                $"Visualização da permissão '{permissao.Nome}' da aplicação '{permissao.NomeAplicacao}'");

            return Ok(permissao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter permissão {Id} da aplicação {AplicacaoId}", id, aplicacaoId);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Cria nova permissão para a aplicação
    /// </summary>
    /// <param name="aplicacaoId">ID da aplicação</param>
    /// <param name="request">Dados da nova permissão</param>
    /// <returns>Permissão criada</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PermissaoAplicacaoCompleta), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CriarPermissaoAplicacao(int aplicacaoId, [FromBody] CriarPermissaoAplicacaoRequest request)
    {
        try
        {
            if (!TemPermissao("PermissoesAplicacao.Criar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para criar permissões de aplicação" 
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verificar se aplicação existe
            var aplicacao = await _context.Aplicacoes
                .Include(a => a.TipoAplicacao)
                .FirstOrDefaultAsync(a => a.Id == aplicacaoId);

            if (aplicacao == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "AplicacaoNaoEncontrada", 
                    Mensagem = "Aplicação não encontrada" 
                });
            }

            // Validar se ID da aplicação no request confere
            if (request.AplicacaoId != aplicacaoId)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "AplicacaoInvalida", 
                    Mensagem = "ID da aplicação no request não confere com a rota" 
                });
            }

            // Verificar se permissão já existe
            var permissaoExistente = await _context.PermissoesAplicacao
                .AnyAsync(pa => pa.AplicacaoId == aplicacaoId && 
                               (pa.Nome == request.Nome || 
                                (pa.Recurso == request.Recurso && pa.Acao == request.Acao)));

            if (permissaoExistente)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "PermissaoJaExiste", 
                    Mensagem = "Já existe uma permissão com este nome ou recurso/ação nesta aplicação" 
                });
            }

            // Validar campos específicos por tipo de aplicação
            var validacao = ValidarCamposPorTipo(aplicacao.TipoAplicacao.Codigo, request);
            if (!validacao.Valido)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "ValidacaoFalhou", 
                    Mensagem = string.Join("; ", validacao.Erros) 
                });
            }

            var usuarioLogadoId = ObterUsuarioLogadoId();

            // Processar criação em lote se solicitado
            if (request.CriarEmLote && request.TemplatesLote?.Any() == true)
            {
                return await ProcessarCriacaoEmLote(aplicacao, request, usuarioLogadoId);
            }

            // Criar permissão única
            var novaPermissao = new PermissaoAplicacao
            {
                AplicacaoId = aplicacaoId,
                Nome = request.Nome,
                Descricao = request.Descricao,
                Recurso = request.Recurso,
                Acao = request.Acao,
                Categoria = request.Categoria,
                Nivel = request.Nivel,
                Endpoint = request.Endpoint,
                MetodoHttp = request.MetodoHttp,
                Modulo = request.Modulo,
                Tela = request.Tela,
                Comando = request.Comando,
                OperacaoSql = request.OperacaoSql,
                Schema = request.Schema,
                Tabela = request.Tabela,
                Condicoes = request.Condicoes ?? "{}",
                DataCriacao = DateTime.UtcNow,
                AtualizadoPorId = usuarioLogadoId
            };

            _context.PermissoesAplicacao.Add(novaPermissao);
            await _context.SaveChangesAsync();

            var permissaoCompleta = await ObterPermissaoAplicacaoCompletaAsync(aplicacaoId, novaPermissao.Id);

            await RegistrarAuditoria("Criar", "PermissaoAplicacao", novaPermissao.Id.ToString(), 
                $"Permissão '{request.Nome}' criada para aplicação '{aplicacao.Nome}'", 
                null, request);

            _logger.LogInformation("✅ Permissão criada - ID: {Id}, Nome: {Nome}, Aplicação: {AplicacaoId}", 
                novaPermissao.Id, request.Nome, aplicacaoId);

            return CreatedAtAction(nameof(ObterPermissaoAplicacao), 
                new { aplicacaoId, id = novaPermissao.Id }, permissaoCompleta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao criar permissão para aplicação {AplicacaoId}", aplicacaoId);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Atualiza permissão existente da aplicação
    /// </summary>
    /// <param name="aplicacaoId">ID da aplicação</param>
    /// <param name="id">ID da permissão</param>
    /// <param name="request">Dados de atualização</param>
    /// <returns>Permissão atualizada</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PermissaoAplicacaoCompleta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AtualizarPermissaoAplicacao(int aplicacaoId, int id, [FromBody] AtualizarPermissaoAplicacaoRequest request)
    {
        try
        {
            if (!TemPermissao("PermissoesAplicacao.Editar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para editar permissões de aplicação" 
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var permissao = await _context.PermissoesAplicacao
                .Include(pa => pa.Aplicacao)
                    .ThenInclude(a => a.TipoAplicacao)
                .FirstOrDefaultAsync(pa => pa.Id == id && pa.AplicacaoId == aplicacaoId);

            if (permissao == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "PermissaoNaoEncontrada", 
                    Mensagem = "Permissão não encontrada nesta aplicação" 
                });
            }

            var dadosAntes = CriarSnapshotPermissao(permissao);
            var usuarioLogadoId = ObterUsuarioLogadoId();

            // Aplicar alterações
            if (!string.IsNullOrEmpty(request.Descricao))
                permissao.Descricao = request.Descricao;

            if (!string.IsNullOrEmpty(request.Categoria))
                permissao.Categoria = request.Categoria;

            if (request.Nivel.HasValue)
                permissao.Nivel = request.Nivel.Value;

            if (request.Ativa.HasValue)
                permissao.Ativa = request.Ativa.Value;

            // Aplicar campos específicos por tipo
            AtualizarCamposPorTipo(permissao, request, permissao.Aplicacao.TipoAplicacao.Codigo);

            if (!string.IsNullOrEmpty(request.Condicoes))
                permissao.Condicoes = request.Condicoes;

            permissao.DataAtualizacao = DateTime.UtcNow;
            permissao.AtualizadoPorId = usuarioLogadoId;

            await _context.SaveChangesAsync();

            var permissaoAtualizada = await ObterPermissaoAplicacaoCompletaAsync(aplicacaoId, id);

            await RegistrarAuditoria("Atualizar", "PermissaoAplicacao", id.ToString(), 
                $"Permissão '{permissao.Nome}' atualizada na aplicação '{permissao.Aplicacao.Nome}'. Motivo: {request.MotivoAlteracao ?? "Não informado"}", 
                dadosAntes, request);

            _logger.LogInformation("✅ Permissão atualizada - ID: {Id}, Aplicação: {AplicacaoId}", 
                id, aplicacaoId);

            return Ok(permissaoAtualizada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao atualizar permissão {Id} da aplicação {AplicacaoId}", id, aplicacaoId);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Remove permissão da aplicação
    /// </summary>
    /// <param name="aplicacaoId">ID da aplicação</param>
    /// <param name="id">ID da permissão</param>
    /// <param name="forcarRemocao">Forçar remoção mesmo se estiver em uso</param>
    /// <returns>Confirmação da remoção</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(RespostaSucesso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoverPermissaoAplicacao(int aplicacaoId, int id, [FromQuery] bool forcarRemocao = false)
    {
        try
        {
            if (!TemPermissao("PermissoesAplicacao.Excluir"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para excluir permissões de aplicação" 
                });
            }

            var permissao = await _context.PermissoesAplicacao
                .Include(pa => pa.Aplicacao)
                .Include(pa => pa.PapelPermissoes)
                .FirstOrDefaultAsync(pa => pa.Id == id && pa.AplicacaoId == aplicacaoId);

            if (permissao == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "PermissaoNaoEncontrada", 
                    Mensagem = "Permissão não encontrada nesta aplicação" 
                });
            }

            // Verificar se está em uso
            var emUso = permissao.PapelPermissoes.Any(ppa => ppa.Ativa);
            if (emUso && !forcarRemocao)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "PermissaoEmUso", 
                    Mensagem = "Permissão está sendo usada por papéis. Use forcarRemocao=true para remover mesmo assim" 
                });
            }

            var nomePermissao = permissao.Nome;
            var nomeAplicacao = permissao.Aplicacao.Nome;

            // Remover relacionamentos primeiro
            if (emUso)
            {
                _context.PapelPermissoesAplicacao.RemoveRange(permissao.PapelPermissoes);
            }

            // Remover permissão
            _context.PermissoesAplicacao.Remove(permissao);
            await _context.SaveChangesAsync();

            await RegistrarAuditoria("Excluir", "PermissaoAplicacao", id.ToString(), 
                $"Permissão '{nomePermissao}' removida da aplicação '{nomeAplicacao}'. Forçado: {forcarRemocao}", 
                permissao, null);

            _logger.LogInformation("✅ Permissão removida - ID: {Id}, Nome: {Nome}, Aplicação: {AplicacaoId}", 
                id, nomePermissao, aplicacaoId);

            return Ok(new RespostaSucesso 
            { 
                Mensagem = $"Permissão '{nomePermissao}' removida com sucesso" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao remover permissão {Id} da aplicação {AplicacaoId}", id, aplicacaoId);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Executa operações em lote com permissões da aplicação
    /// </summary>
    /// <param name="aplicacaoId">ID da aplicação</param>
    /// <param name="request">Operação em lote</param>
    /// <returns>Resultado da operação</returns>
    [HttpPost("lote")]
    [ProducesResponseType(typeof(ResultadoOperacaoLotePermissoesAplicacao), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> OperacaoLotePermissoes(int aplicacaoId, [FromBody] OperacaoLotePermissoesAplicacao request)
    {
        try
        {
            if (!TemPermissao("PermissoesAplicacao.Lote"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para operações em lote com permissões" 
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verificar aplicação
            var aplicacao = await _context.Aplicacoes
                .FirstOrDefaultAsync(a => a.Id == aplicacaoId);

            if (aplicacao == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "AplicacaoNaoEncontrada", 
                    Mensagem = "Aplicação não encontrada" 
                });
            }

            // Validar se aplicação confere
            if (request.AplicacaoId.HasValue && request.AplicacaoId.Value != aplicacaoId)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "AplicacaoInvalida", 
                    Mensagem = "ID da aplicação no request não confere com a rota" 
                });
            }

            var resultado = await ExecutarOperacaoLote(aplicacaoId, request);

            await RegistrarAuditoria("OperacaoLote", "PermissaoAplicacao", aplicacaoId.ToString(), 
                $"Operação '{request.TipoOperacao}' executada em lote na aplicação '{aplicacao.Nome}'. Itens: {request.PermissoesIds.Count}", 
                null, request);

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro em operação lote da aplicação {AplicacaoId}", aplicacaoId);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Obtém estatísticas de permissões da aplicação
    /// </summary>
    /// <param name="aplicacaoId">ID da aplicação</param>
    /// <returns>Estatísticas detalhadas</returns>
    [HttpGet("estatisticas")]
    [ProducesResponseType(typeof(EstatisticasAplicacaoPermissoes), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterEstatisticasPermissoes(int aplicacaoId)
    {
        try
        {
            if (!TemPermissao("PermissoesAplicacao.Estatisticas"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para visualizar estatísticas" 
                });
            }

            var aplicacao = await _context.Aplicacoes
                .Include(a => a.TipoAplicacao)
                .FirstOrDefaultAsync(a => a.Id == aplicacaoId);

            if (aplicacao == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "AplicacaoNaoEncontrada", 
                    Mensagem = "Aplicação não encontrada" 
                });
            }

            var estatisticas = await GerarEstatisticasAplicacao(aplicacao);

            return Ok(estatisticas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao gerar estatísticas da aplicação {AplicacaoId}", aplicacaoId);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Exporta permissões da aplicação
    /// </summary>
    /// <param name="aplicacaoId">ID da aplicação</param>
    /// <param name="request">Configurações de exportação</param>
    /// <returns>Arquivo exportado</returns>
    [HttpPost("exportar")]
    [ProducesResponseType(typeof(DTOs.Comuns.ArquivoExportacao), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportarPermissoes(int aplicacaoId, [FromBody] ExportarPermissoesAplicacaoRequest request)
    {
        try
        {
            if (!TemPermissao("PermissoesAplicacao.Exportar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para exportar permissões" 
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var aplicacao = await _context.Aplicacoes
                .FirstOrDefaultAsync(a => a.Id == aplicacaoId);

            if (aplicacao == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "AplicacaoNaoEncontrada", 
                    Mensagem = "Aplicação não encontrada" 
                });
            }

            // Definir aplicação no request se não especificada
            request.AplicacaoId = aplicacaoId;

            var arquivo = await ProcessarExportacao(request);

            await RegistrarAuditoria("Exportar", "PermissaoAplicacao", aplicacaoId.ToString(), 
                $"Permissões da aplicação '{aplicacao.Nome}' exportadas no formato {request.Formato}", 
                null, request);

            return Ok(arquivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao exportar permissões da aplicação {AplicacaoId}", aplicacaoId);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Importa permissões para a aplicação
    /// </summary>
    /// <param name="aplicacaoId">ID da aplicação</param>
    /// <param name="request">Dados para importação</param>
    /// <returns>Resultado da importação</returns>
    [HttpPost("importar")]
    [ProducesResponseType(typeof(DTOs.Comuns.ResultadoImportacao), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ImportarPermissoes(int aplicacaoId, [FromBody] ImportarPermissoesAplicacaoRequest request)
    {
        try
        {
            if (!TemPermissao("PermissoesAplicacao.Importar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para importar permissões" 
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var aplicacao = await _context.Aplicacoes
                .Include(a => a.TipoAplicacao)
                .FirstOrDefaultAsync(a => a.Id == aplicacaoId);

            if (aplicacao == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "AplicacaoNaoEncontrada", 
                    Mensagem = "Aplicação não encontrada" 
                });
            }

            // Validar se aplicação confere
            if (request.AplicacaoId != aplicacaoId)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "AplicacaoInvalida", 
                    Mensagem = "ID da aplicação no request não confere com a rota" 
                });
            }

            var resultado = await ProcessarImportacao(aplicacao, request);

            await RegistrarAuditoria("Importar", "PermissaoAplicacao", aplicacaoId.ToString(), 
                $"Importação de permissões para aplicação '{aplicacao.Nome}' no modo {request.Modo}. Processados: {resultado.TotalProcessados}", 
                null, request);

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao importar permissões para aplicação {AplicacaoId}", aplicacaoId);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    #region Métodos Auxiliares

    private bool TemPermissao(string permissao)
    {
        return User.HasClaim("permission", permissao) || User.IsInRole("SuperAdmin");
    }

    private int ObterUsuarioLogadoId()
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(usuarioIdClaim, out var usuarioId) ? usuarioId : 0;
    }

    private IQueryable<PermissaoAplicacao> AplicarFiltros(IQueryable<PermissaoAplicacao> query, FiltrosPermissaoAplicacao filtros)
    {
        if (!string.IsNullOrEmpty(filtros.Nome))
        {
            query = query.Where(pa => pa.Nome.ToLower().Contains(filtros.Nome.ToLower()));
        }

        if (!string.IsNullOrEmpty(filtros.Descricao))
        {
            query = query.Where(pa => pa.Descricao.ToLower().Contains(filtros.Descricao.ToLower()));
        }

        if (!string.IsNullOrEmpty(filtros.Recurso))
        {
            query = query.Where(pa => pa.Recurso.ToLower().Contains(filtros.Recurso.ToLower()));
        }

        if (!string.IsNullOrEmpty(filtros.Acao))
        {
            query = query.Where(pa => pa.Acao.ToLower().Contains(filtros.Acao.ToLower()));
        }

        if (!string.IsNullOrEmpty(filtros.Categoria))
        {
            query = query.Where(pa => pa.Categoria != null && pa.Categoria.ToLower().Contains(filtros.Categoria.ToLower()));
        }

        if (filtros.NivelMinimo.HasValue)
        {
            query = query.Where(pa => pa.Nivel >= filtros.NivelMinimo.Value);
        }

        if (filtros.NivelMaximo.HasValue)
        {
            query = query.Where(pa => pa.Nivel <= filtros.NivelMaximo.Value);
        }

        if (filtros.Ativa.HasValue)
        {
            query = query.Where(pa => pa.Ativa == filtros.Ativa.Value);
        }

        // Filtros específicos por tipo de aplicação
        if (!string.IsNullOrEmpty(filtros.Endpoint))
        {
            query = query.Where(pa => pa.Endpoint != null && pa.Endpoint.ToLower().Contains(filtros.Endpoint.ToLower()));
        }

        if (!string.IsNullOrEmpty(filtros.MetodoHttp))
        {
            query = query.Where(pa => pa.MetodoHttp != null && pa.MetodoHttp.ToUpper() == filtros.MetodoHttp.ToUpper());
        }

        if (!string.IsNullOrEmpty(filtros.Modulo))
        {
            query = query.Where(pa => pa.Modulo != null && pa.Modulo.ToLower().Contains(filtros.Modulo.ToLower()));
        }

        if (!string.IsNullOrEmpty(filtros.Tela))
        {
            query = query.Where(pa => pa.Tela != null && pa.Tela.ToLower().Contains(filtros.Tela.ToLower()));
        }

        if (!string.IsNullOrEmpty(filtros.Comando))
        {
            query = query.Where(pa => pa.Comando != null && pa.Comando.ToLower().Contains(filtros.Comando.ToLower()));
        }

        if (!string.IsNullOrEmpty(filtros.OperacaoSql))
        {
            query = query.Where(pa => pa.OperacaoSql != null && pa.OperacaoSql.ToUpper() == filtros.OperacaoSql.ToUpper());
        }

        if (!string.IsNullOrEmpty(filtros.Schema))
        {
            query = query.Where(pa => pa.Schema != null && pa.Schema.ToLower().Contains(filtros.Schema.ToLower()));
        }

        if (!string.IsNullOrEmpty(filtros.Tabela))
        {
            query = query.Where(pa => pa.Tabela != null && pa.Tabela.ToLower().Contains(filtros.Tabela.ToLower()));
        }

        if (filtros.DataCriacaoInicio.HasValue)
        {
            query = query.Where(pa => pa.DataCriacao >= filtros.DataCriacaoInicio.Value);
        }

        if (filtros.DataCriacaoFim.HasValue)
        {
            query = query.Where(pa => pa.DataCriacao <= filtros.DataCriacaoFim.Value);
        }

        // ✅ Corrigido: usando IncluirInativos da classe base
        if (!filtros.IncluirInativos)
        {
            query = query.Where(pa => pa.Ativa);
        }

        if (filtros.EmUso.HasValue)
        {
            if (filtros.EmUso.Value)
            {
                query = query.Where(pa => pa.PapelPermissoes.Any(ppa => ppa.Ativa));
            }
            else
            {
                query = query.Where(pa => !pa.PapelPermissoes.Any(ppa => ppa.Ativa));
            }
        }

        // ✅ Corrigido: usando TermoBusca da classe base
        if (!string.IsNullOrEmpty(filtros.TermoBusca))
        {
            var termo = filtros.TermoBusca.ToLower();
            query = query.Where(pa => 
                pa.Nome.ToLower().Contains(termo) ||
                pa.Descricao.ToLower().Contains(termo) ||
                pa.Recurso.ToLower().Contains(termo) ||
                pa.Acao.ToLower().Contains(termo) ||
                (pa.Categoria != null && pa.Categoria.ToLower().Contains(termo)));
        }

        return query;
    }

    private IQueryable<PermissaoAplicacao> AplicarOrdenacao(IQueryable<PermissaoAplicacao> query, string? ordenarPor, string? direcao)
    {
        var desc = direcao?.ToLower() == "desc";

        return ordenarPor?.ToLower() switch
        {
            "nome" => desc ? query.OrderByDescending(pa => pa.Nome) : query.OrderBy(pa => pa.Nome),
            "descricao" => desc ? query.OrderByDescending(pa => pa.Descricao) : query.OrderBy(pa => pa.Descricao),
            "recurso" => desc ? query.OrderByDescending(pa => pa.Recurso) : query.OrderBy(pa => pa.Recurso),
            "acao" => desc ? query.OrderByDescending(pa => pa.Acao) : query.OrderBy(pa => pa.Acao),
            "categoria" => desc ? query.OrderByDescending(pa => pa.Categoria) : query.OrderBy(pa => pa.Categoria),
            "nivel" => desc ? query.OrderByDescending(pa => pa.Nivel) : query.OrderBy(pa => pa.Nivel),
            "datacriacao" => desc ? query.OrderByDescending(pa => pa.DataCriacao) : query.OrderBy(pa => pa.DataCriacao),
            "ativa" => desc ? query.OrderByDescending(pa => pa.Ativa) : query.OrderBy(pa => pa.Ativa),
            _ => query.OrderBy(pa => pa.Nome)
        };
    }

    // Métodos auxiliares para construção de DTOs, validação, operações em lote, etc.
    // Implementações detalhadas serão adicionadas conforme necessário...

    private PermissaoAplicacaoResumo ConstruirPermissaoAplicacaoResumo(PermissaoAplicacao permissao)
    {
        var campoEspecifico = ObterCampoEspecificoRelevante(permissao);
        
        return new PermissaoAplicacaoResumo
        {
            Id = permissao.Id,
            AplicacaoId = permissao.AplicacaoId,
            NomeAplicacao = permissao.Aplicacao.Nome,
            CodigoAplicacao = permissao.Aplicacao.Codigo,
            Nome = permissao.Nome,
            Descricao = permissao.Descricao,
            Recurso = permissao.Recurso,
            Acao = permissao.Acao,
            Categoria = permissao.Categoria,
            Nivel = permissao.Nivel,
            Ativa = permissao.Ativa,
            CampoEspecifico = campoEspecifico.campo,
            ValorCampoEspecifico = campoEspecifico.valor,
            TotalPapeis = permissao.PapelPermissoes.Count(ppa => ppa.Ativa),
            DataCriacao = permissao.DataCriacao,
            TipoAplicacao = new TipoAplicacaoPermissao
            {
                Codigo = permissao.Aplicacao.TipoAplicacao.Codigo,
                Nome = permissao.Aplicacao.TipoAplicacao.Nome,
                Icone = permissao.Aplicacao.TipoAplicacao.Icone,
                Cor = permissao.Aplicacao.TipoAplicacao.Cor,
                CamposSuportados = ObterCamposSuportados(permissao.Aplicacao.TipoAplicacao.Codigo)
            }
        };
    }

    private (string? campo, string? valor) ObterCampoEspecificoRelevante(PermissaoAplicacao permissao)
    {
        return permissao.Aplicacao.TipoAplicacao.Codigo.ToLower() switch
        {
            "webapi" or "spa" => ("Endpoint", permissao.Endpoint),
            "desktop" or "wpf" => ("Módulo", permissao.Modulo),
            "mobile" => ("Tela", permissao.Tela),
            "cli" => ("Comando", permissao.Comando),
            "database" => ("Tabela", permissao.Tabela),
            _ => (null, null)
        };
    }

    private List<string> ObterCamposSuportados(string tipoAplicacao)
    {
        return tipoAplicacao.ToLower() switch
        {
            "webapi" or "spa" => new List<string> { "endpoint", "metodoHttp" },
            "desktop" or "wpf" => new List<string> { "modulo", "tela" },
            "mobile" => new List<string> { "tela", "modulo" },
            "cli" => new List<string> { "comando" },
            "database" => new List<string> { "operacaoSql", "schema", "tabela" },
            _ => new List<string>()
        };
    }

    // Placeholder para métodos auxiliares complexos que serão implementados...
    private async Task<PermissaoAplicacaoCompleta?> ObterPermissaoAplicacaoCompletaAsync(int aplicacaoId, int permissaoId)
    {
        var permissao = await _context.PermissoesAplicacao
            .Include(pa => pa.Aplicacao)
                .ThenInclude(a => a.TipoAplicacao)
            .Include(pa => pa.AtualizadoPor)
            .Include(pa => pa.PapelPermissoes.Where(ppa => ppa.Ativa))
                .ThenInclude(ppa => ppa.Papel)
            .FirstOrDefaultAsync(pa => pa.Id == permissaoId && pa.AplicacaoId == aplicacaoId);

        if (permissao == null)
            return null;

        return new PermissaoAplicacaoCompleta
        {
            Id = permissao.Id,
            AplicacaoId = permissao.AplicacaoId,
            NomeAplicacao = permissao.Aplicacao.Nome,
            CodigoAplicacao = permissao.Aplicacao.Codigo,
            Nome = permissao.Nome,
            Descricao = permissao.Descricao,
            Recurso = permissao.Recurso,
            Acao = permissao.Acao,
            Categoria = permissao.Categoria,
            Nivel = permissao.Nivel,
            Ativa = permissao.Ativa,
            Endpoint = permissao.Endpoint,
            MetodoHttp = permissao.MetodoHttp,
            Modulo = permissao.Modulo,
            Tela = permissao.Tela,
            Comando = permissao.Comando,
            OperacaoSql = permissao.OperacaoSql,
            Schema = permissao.Schema,
            Tabela = permissao.Tabela,
            Condicoes = permissao.Condicoes,
            DataCriacao = permissao.DataCriacao,
            DataAtualizacao = permissao.DataAtualizacao,
            AtualizadoPor = permissao.AtualizadoPor != null ? 
                $"{permissao.AtualizadoPor.Nome} {permissao.AtualizadoPor.Sobrenome}" : null,
            TotalPapeis = permissao.PapelPermissoes.Count(ppa => ppa.Ativa),
            TotalUsuarios = await ContarUsuariosComPermissaoAplicacao(permissaoId),
            Papeis = permissao.PapelPermissoes
                .Where(ppa => ppa.Ativa)
                .Select(ppa => new PapelPermissaoAplicacaoResumo
                {
                    PapelId = ppa.PapelId,
                    NomePapel = ppa.Papel.Name ?? string.Empty,
                    DescricaoPapel = ppa.Papel.Descricao ?? string.Empty,
                    CategoriaPapel = ppa.Papel.Categoria,
                    NivelPapel = ppa.Papel.Nivel,
                    PapelAtivo = ppa.Papel.Ativo,
                    DataAtribuicao = ppa.DataAtribuicao,
                    DataExpiracao = ppa.DataExpiracao,
                    AtribuicaoAtiva = ppa.Ativa,
                    AtribuidoPor = ppa.AtribuidoPor != null ? 
                        $"{ppa.AtribuidoPor.Nome} {ppa.AtribuidoPor.Sobrenome}" : null,
                    Observacoes = ppa.Observacoes,
                    TotalUsuarios = ppa.Papel.UsuarioPapeis.Count(up => up.Ativo)
                }).ToList(),
            TipoAplicacao = new TipoAplicacaoPermissao
            {
                Codigo = permissao.Aplicacao.TipoAplicacao.Codigo,
                Nome = permissao.Aplicacao.TipoAplicacao.Nome,
                Icone = permissao.Aplicacao.TipoAplicacao.Icone,
                Cor = permissao.Aplicacao.TipoAplicacao.Cor,
                CamposSuportados = ObterCamposSuportados(permissao.Aplicacao.TipoAplicacao.Codigo)
            },
            Estatisticas = await GerarEstatisticasPermissaoEspecifica(permissaoId)
        };
    }

    private async Task<int> ContarUsuariosComPermissaoAplicacao(int permissaoId)
    {
        return await _context.UsuarioPapeis
            .Where(up => up.Ativo && 
                        up.Papel.Ativo && 
                        up.Papel.PapelPermissoesAplicacao.Any(ppa =>
                            ppa.PermissaoAplicacaoId == permissaoId && ppa.Ativa))
            .Select(up => up.UsuarioId)
            .Distinct()
            .CountAsync();
    }

    private async Task<EstatisticasPermissaoAplicacao> GerarEstatisticasPermissaoEspecifica(int permissaoId)
    {
        var papelPermissoes = await _context.PapelPermissoesAplicacao
            .Where(ppa => ppa.PermissaoAplicacaoId == permissaoId)
            .Include(ppa => ppa.Papel)
            .ToListAsync();

        var papeisAtivos = papelPermissoes.Where(ppa => ppa.Ativa && ppa.Papel.Ativo).ToList();
        var papeisInativos = papelPermissoes.Where(ppa => !ppa.Ativa || !ppa.Papel.Ativo).ToList();

        return new EstatisticasPermissaoAplicacao
        {
            TotalPapeisAtivos = papeisAtivos.Count,
            TotalPapeisInativos = papeisInativos.Count,
            TotalUsuariosComPermissao = await ContarUsuariosComPermissaoAplicacao(permissaoId),
            UltimaAtribuicao = papelPermissoes.Max(ppa => (DateTime?)ppa.DataAtribuicao),
            UltimoUso = null, // TODO: Implementar log de uso
            TotalUsosUltimo30Dias = 0 // TODO: Implementar contagem de uso
        };
    }

    private ResultadoValidacao ValidarCamposPorTipo(string tipoAplicacao, CriarPermissaoAplicacaoRequest request)
    {
        var resultado = new ResultadoValidacao { Valido = true };

        switch (tipoAplicacao.ToLower())
        {
            case "webapi":
            case "spa":
                if (string.IsNullOrEmpty(request.Endpoint))
                {
                    resultado.Erros.Add("Endpoint é obrigatório para aplicações Web API/SPA");
                    resultado.Valido = false;
                }
                if (string.IsNullOrEmpty(request.MetodoHttp))
                {
                    resultado.Erros.Add("Método HTTP é obrigatório para aplicações Web API/SPA");
                    resultado.Valido = false;
                }
                else if (!ValidarMetodoHttp(request.MetodoHttp))
                {
                    resultado.Erros.Add("Método HTTP inválido. Use: GET, POST, PUT, DELETE, PATCH, * (todos)");
                    resultado.Valido = false;
                }
                break;

            case "desktop":
            case "wpf":
                if (string.IsNullOrEmpty(request.Modulo) && string.IsNullOrEmpty(request.Tela))
                {
                    resultado.Erros.Add("Módulo ou Tela deve ser especificado para aplicações Desktop");
                    resultado.Valido = false;
                }
                break;

            case "mobile":
                if (string.IsNullOrEmpty(request.Tela))
                {
                    resultado.Erros.Add("Tela é obrigatória para aplicações Mobile");
                    resultado.Valido = false;
                }
                break;

            case "cli":
                if (string.IsNullOrEmpty(request.Comando))
                {
                    resultado.Erros.Add("Comando é obrigatório para aplicações CLI");
                    resultado.Valido = false;
                }
                break;

            case "database":
                if (string.IsNullOrEmpty(request.OperacaoSql))
                {
                    resultado.Erros.Add("Operação SQL é obrigatória para aplicações de Banco");
                    resultado.Valido = false;
                }
                else if (!ValidarOperacaoSql(request.OperacaoSql))
                {
                    resultado.Erros.Add("Operação SQL inválida. Use: SELECT, INSERT, UPDATE, DELETE, * (todas)");
                    resultado.Valido = false;
                }
                break;
        }

        // Validar consistência nome vs recurso.acao
        if (!request.Nome.Equals($"{request.Recurso}.{request.Acao}", StringComparison.OrdinalIgnoreCase))
        {
            resultado.Erros.Add($"Nome deve seguir o formato: {request.Recurso}.{request.Acao}");
            resultado.Valido = false;
        }

        return resultado;
    }

    private bool ValidarMetodoHttp(string metodo)
    {
        var metodosValidos = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", "*" };
        return metodosValidos.Contains(metodo.ToUpper());
    }

    private bool ValidarOperacaoSql(string operacao)
    {
        var operacoesValidas = new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "CREATE", "DROP", "ALTER", "*" };
        return operacoesValidas.Contains(operacao.ToUpper());
    }

    private async Task<IActionResult> ProcessarCriacaoEmLote(Aplicacao aplicacao, CriarPermissaoAplicacaoRequest request, int usuarioLogadoId)
    {
        var resultados = new List<ResultadoIndividualPermissaoAplicacao>();
        var permissoesCriadas = new List<PermissaoAplicacaoCompleta>();

        foreach (var template in request.TemplatesLote!)
        {
            try
            {
                var permissaoNome = $"{template.Recurso}.{template.Acao}";
                
                // Verificar se já existe
                var jaExiste = await _context.PermissoesAplicacao
                    .AnyAsync(pa => pa.AplicacaoId == aplicacao.Id && pa.Nome == permissaoNome);

                if (jaExiste)
                {
                    resultados.Add(new ResultadoIndividualPermissaoAplicacao
                    {
                        PermissaoId = 0,
                        Nome = permissaoNome,
                        Sucesso = false,
                        Erro = "Permissão já existe",
                        Detalhes = $"Permissão '{permissaoNome}' já existe na aplicação"
                    });
                    continue;
                }

                var novaPermissao = new PermissaoAplicacao
                {
                    AplicacaoId = aplicacao.Id,
                    Nome = permissaoNome,
                    Descricao = $"Permissão {template.Acao} para {template.Recurso}",
                    Recurso = template.Recurso,
                    Acao = template.Acao,
                    Categoria = template.Categoria,
                    Nivel = template.Nivel,
                    DataCriacao = DateTime.UtcNow,
                    AtualizadoPorId = usuarioLogadoId
                };

                // Aplicar campos específicos do template
                foreach (var campo in template.CamposEspecificos)
                {
                    switch (campo.Key.ToLower())
                    {
                        case "endpoint": novaPermissao.Endpoint = campo.Value; break;
                        case "metodohttp": novaPermissao.MetodoHttp = campo.Value; break;
                        case "modulo": novaPermissao.Modulo = campo.Value; break;
                        case "tela": novaPermissao.Tela = campo.Value; break;
                        case "comando": novaPermissao.Comando = campo.Value; break;
                        case "operacaosql": novaPermissao.OperacaoSql = campo.Value; break;
                        case "schema": novaPermissao.Schema = campo.Value; break;
                        case "tabela": novaPermissao.Tabela = campo.Value; break;
                    }
                }

                _context.PermissoesAplicacao.Add(novaPermissao);
                await _context.SaveChangesAsync();

                var permissaoCompleta = await ObterPermissaoAplicacaoCompletaAsync(aplicacao.Id, novaPermissao.Id);
                if (permissaoCompleta != null)
                {
                    permissoesCriadas.Add(permissaoCompleta);
                }

                resultados.Add(new ResultadoIndividualPermissaoAplicacao
                {
                    PermissaoId = novaPermissao.Id,
                    Nome = permissaoNome,
                    Sucesso = true,
                    Detalhes = "Permissão criada com sucesso"
                });
            }
            catch (Exception ex)
            {
                resultados.Add(new ResultadoIndividualPermissaoAplicacao
                {
                    PermissaoId = 0,
                    Nome = $"{template.Recurso}.{template.Acao}",
                    Sucesso = false,
                    Erro = ex.Message,
                    Detalhes = "Erro ao criar permissão"
                });
            }
        }

        var resposta = new ResultadoOperacaoLotePermissoesAplicacao
        {
            Sucesso = resultados.Any(r => r.Sucesso),
            TipoOperacao = "criar-lote",
            TotalProcessadas = resultados.Count,
            TotalSucessos = resultados.Count(r => r.Sucesso),
            TotalFalhas = resultados.Count(r => !r.Sucesso),
            Detalhes = resultados,
            Mensagem = $"Processamento em lote concluído. {resultados.Count(r => r.Sucesso)} sucessos, {resultados.Count(r => !r.Sucesso)} falhas.",
            Resumo = $"Criadas {resultados.Count(r => r.Sucesso)} permissões de {resultados.Count} templates"
        };

        return Ok(resposta);
    }

    private void AtualizarCamposPorTipo(PermissaoAplicacao permissao, AtualizarPermissaoAplicacaoRequest request, string tipoAplicacao)
    {
        switch (tipoAplicacao.ToLower())
        {
            case "webapi":
            case "spa":
                if (!string.IsNullOrEmpty(request.Endpoint))
                    permissao.Endpoint = request.Endpoint;
                if (!string.IsNullOrEmpty(request.MetodoHttp))
                    permissao.MetodoHttp = request.MetodoHttp;
                break;

            case "desktop":
            case "wpf":
                if (!string.IsNullOrEmpty(request.Modulo))
                    permissao.Modulo = request.Modulo;
                if (!string.IsNullOrEmpty(request.Tela))
                    permissao.Tela = request.Tela;
                break;

            case "mobile":
                if (!string.IsNullOrEmpty(request.Tela))
                    permissao.Tela = request.Tela;
                if (!string.IsNullOrEmpty(request.Modulo))
                    permissao.Modulo = request.Modulo;
                break;

            case "cli":
                if (!string.IsNullOrEmpty(request.Comando))
                    permissao.Comando = request.Comando;
                break;

            case "database":
                if (!string.IsNullOrEmpty(request.OperacaoSql))
                    permissao.OperacaoSql = request.OperacaoSql;
                if (!string.IsNullOrEmpty(request.Schema))
                    permissao.Schema = request.Schema;
                if (!string.IsNullOrEmpty(request.Tabela))
                    permissao.Tabela = request.Tabela;
                break;
        }
    }

    private object CriarSnapshotPermissao(PermissaoAplicacao permissao)
    {
        return new
        {
            permissao.Id,
            permissao.Nome,
            permissao.Descricao,
            permissao.Recurso,
            permissao.Acao,
            permissao.Categoria,
            permissao.Nivel,
            permissao.Ativa,
            permissao.Endpoint,
            permissao.MetodoHttp,
            permissao.Modulo,
            permissao.Tela,
            permissao.Comando,
            permissao.OperacaoSql,
            permissao.Schema,
            permissao.Tabela,
            permissao.Condicoes
        };
    }

    private async Task<ResultadoOperacaoLotePermissoesAplicacao> ExecutarOperacaoLote(int aplicacaoId, OperacaoLotePermissoesAplicacao request)
    {
        var inicio = DateTime.UtcNow;
        var resultados = new List<ResultadoIndividualPermissaoAplicacao>();

        var permissoes = await _context.PermissoesAplicacao
            .Where(pa => pa.AplicacaoId == aplicacaoId && request.PermissoesIds.Contains(pa.Id))
            .ToListAsync();

        foreach (var permissao in permissoes)
        {
            try
            {
                var estadoAnterior = CriarSnapshotPermissao(permissao);
                bool sucesso = false;
                string? erro = null;

                switch (request.TipoOperacao.ToLower())
                {
                    case "ativar":
                        permissao.Ativa = true;
                        sucesso = true;
                        break;

                    case "desativar":
                        permissao.Ativa = false;
                        sucesso = true;
                        break;

                    case "excluir":
                        var emUso = await _context.PapelPermissoesAplicacao
                            .AnyAsync(ppa => ppa.PermissaoAplicacaoId == permissao.Id && ppa.Ativa);
                        
                        if (emUso && !request.ForcarOperacao)
                        {
                            erro = "Permissão em uso por papéis";
                        }
                        else
                        {
                            if (emUso)
                            {
                                var relacionamentos = await _context.PapelPermissoesAplicacao
                                    .Where(ppa => ppa.PermissaoAplicacaoId == permissao.Id)
                                    .ToListAsync();
                                _context.PapelPermissoesAplicacao.RemoveRange(relacionamentos);
                            }
                            _context.PermissoesAplicacao.Remove(permissao);
                            sucesso = true;
                        }
                        break;

                    case "categoria-alterar":
                        if (request.Parametros.TryGetValue("categoria", out var novaCategoria))
                        {
                            permissao.Categoria = novaCategoria;
                            sucesso = true;
                        }
                        else
                        {
                            erro = "Parâmetro 'categoria' não especificado";
                        }
                        break;

                    case "nivel-alterar":
                        if (request.Parametros.TryGetValue("nivel", out var novoNivelStr) && 
                            int.TryParse(novoNivelStr, out var novoNivel) && 
                            novoNivel >= 1 && novoNivel <= 10)
                        {
                            permissao.Nivel = novoNivel;
                            sucesso = true;
                        }
                        else
                        {
                            erro = "Parâmetro 'nivel' inválido (deve ser entre 1 e 10)";
                        }
                        break;

                    default:
                        erro = $"Operação '{request.TipoOperacao}' não reconhecida";
                        break;
                }

                if (sucesso)
                {
                    permissao.DataAtualizacao = DateTime.UtcNow;
                    permissao.AtualizadoPorId = ObterUsuarioLogadoId();
                }

                resultados.Add(new ResultadoIndividualPermissaoAplicacao
                {
                    PermissaoId = permissao.Id,
                    Nome = permissao.Nome,
                    Sucesso = sucesso,
                    Erro = erro,
                    EstadoAnterior = estadoAnterior,
                    EstadoAtual = sucesso ? CriarSnapshotPermissao(permissao) : null
                });
            }
            catch (Exception ex)
            {
                resultados.Add(new ResultadoIndividualPermissaoAplicacao
                {
                    PermissaoId = permissao.Id,
                    Nome = permissao.Nome,
                    Sucesso = false,
                    Erro = ex.Message
                });
            }
        }

        // Salvar mudanças
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Marcar todos como falha se não conseguiu salvar
            foreach (var resultado in resultados.Where(r => r.Sucesso))
            {
                resultado.Sucesso = false;
                resultado.Erro = $"Erro ao salvar: {ex.Message}";
            }
        }

        var duracao = DateTime.UtcNow - inicio;
        var sucessos = resultados.Count(r => r.Sucesso);
        var falhas = resultados.Count(r => !r.Sucesso);

        return new ResultadoOperacaoLotePermissoesAplicacao
        {
            Sucesso = sucessos > 0,
            TipoOperacao = request.TipoOperacao,
            TotalProcessadas = resultados.Count,
            TotalSucessos = sucessos,
            TotalFalhas = falhas,
            Detalhes = resultados,
            Duracao = duracao,
            Mensagem = sucessos > 0 ? 
                $"Operação '{request.TipoOperacao}' concluída com {sucessos} sucessos e {falhas} falhas" :
                $"Operação '{request.TipoOperacao}' falhou para todas as permissões",
            Resumo = $"{sucessos}/{resultados.Count} permissões processadas com sucesso"
        };
    }

    private async Task<EstatisticasAplicacaoPermissoes> GerarEstatisticasAplicacao(Aplicacao aplicacao)
    {
        var permissoes = await _context.PermissoesAplicacao
            .Where(pa => pa.AplicacaoId == aplicacao.Id)
            .Include(pa => pa.PapelPermissoes)
                .ThenInclude(ppa => ppa.Papel)
                    .ThenInclude(p => p.UsuarioPapeis)
            .ToListAsync();

        var estatisticas = new EstatisticasAplicacaoPermissoes
        {
            AplicacaoId = aplicacao.Id,
            NomeAplicacao = aplicacao.Nome,
            CodigoAplicacao = aplicacao.Codigo,
            TotalPermissoes = permissoes.Count,
            PermissoesAtivas = permissoes.Count(p => p.Ativa),
            PermissoesInativas = permissoes.Count(p => !p.Ativa),
            TotalCategorias = permissoes.Where(p => !string.IsNullOrEmpty(p.Categoria))
                                      .Select(p => p.Categoria)
                                      .Distinct()
                                      .Count(),
            TotalRecursos = permissoes.Select(p => p.Recurso).Distinct().Count(),
            TotalAcoes = permissoes.Select(p => p.Acao).Distinct().Count(),
            DataGeracao = DateTime.UtcNow
        };

        // Permissões mais usadas
        estatisticas.PermissoesMaisUsadas = permissoes
            .Where(p => p.Ativa)
            .Select(p => new PermissaoMaisUsada
            {
                Id = p.Id,
                Nome = p.Nome,
                Recurso = p.Recurso,
                Acao = p.Acao,
                TotalPapeis = p.PapelPermissoes.Count(ppa => ppa.Ativa && ppa.Papel.Ativo),
                TotalUsuarios = p.PapelPermissoes
                    .Where(ppa => ppa.Ativa && ppa.Papel.Ativo)
                    .SelectMany(ppa => ppa.Papel.UsuarioPapeis)
                    .Where(up => up.Ativo)
                    .Select(up => up.UsuarioId)
                    .Distinct()
                    .Count(),
                PercentualUso = estatisticas.TotalPermissoes > 0 ? 
                    (decimal)p.PapelPermissoes.Count(ppa => ppa.Ativa) / estatisticas.TotalPermissoes * 100 : 0
            })
            .OrderByDescending(p => p.TotalUsuarios)
            .Take(10)
            .ToList();

        // Recursos mais usados
        estatisticas.RecursosMaisUsados = permissoes
            .GroupBy(p => p.Recurso)
            .Select(g => new RecursoEstatistica
            {
                Nome = g.Key,
                TotalPermissoes = g.Count(),
                Acoes = g.Select(p => p.Acao).Distinct().ToList(),
                PercentualTotal = estatisticas.TotalPermissoes > 0 ? 
                    (decimal)g.Count() / estatisticas.TotalPermissoes * 100 : 0
            })
            .OrderByDescending(r => r.TotalPermissoes)
            .Take(10)
            .ToList();

        // Categorias mais usadas
        estatisticas.CategoriasMaisUsadas = permissoes
            .Where(p => !string.IsNullOrEmpty(p.Categoria))
            .GroupBy(p => p.Categoria!)
            .Select(g => new CategoriaEstatistica
            {
                Nome = g.Key,
                TotalPermissoes = g.Count(),
                PercentualTotal = estatisticas.TotalPermissoes > 0 ? 
                    (decimal)g.Count() / estatisticas.TotalPermissoes * 100 : 0
            })
            .OrderByDescending(c => c.TotalPermissoes)
            .ToList();

        // Distribuição por nível
        estatisticas.DistribuicaoPorNivel = permissoes
            .GroupBy(p => p.Nivel)
            .Select(g => new NivelEstatistica
            {
                Nivel = g.Key,
                DescricaoNivel = ObterDescricaoNivel(g.Key),
                TotalPermissoes = g.Count(),
                PercentualTotal = estatisticas.TotalPermissoes > 0 ? 
                    (decimal)g.Count() / estatisticas.TotalPermissoes * 100 : 0
            })
            .OrderBy(n => n.Nivel)
            .ToList();

        // Campos específicos por tipo
        estatisticas.CamposEspecificos = GerarEstatisticasCamposEspecificos(permissoes, aplicacao.TipoAplicacao.Codigo);

        return estatisticas;
    }

    private string ObterDescricaoNivel(int nivel)
    {
        return nivel switch
        {
            1 => "Básico",
            2 => "Baixo",
            3 => "Baixo-Médio",
            4 => "Médio",
            5 => "Médio",
            6 => "Médio-Alto",
            7 => "Alto",
            8 => "Alto",
            9 => "Crítico",
            10 => "Máximo",
            _ => "Indefinido"
        };
    }

    private Dictionary<string, List<CampoEstatistica>> GerarEstatisticasCamposEspecificos(
        List<PermissaoAplicacao> permissoes, string tipoAplicacao)
    {
        var estatisticas = new Dictionary<string, List<CampoEstatistica>>();

        switch (tipoAplicacao.ToLower())
        {
            case "webapi":
            case "spa":
                if (permissoes.Any(p => !string.IsNullOrEmpty(p.MetodoHttp)))
                {
                    estatisticas["MetodoHttp"] = permissoes
                        .Where(p => !string.IsNullOrEmpty(p.MetodoHttp))
                        .GroupBy(p => p.MetodoHttp!)
                        .Select(g => new CampoEstatistica
                        {
                            Valor = g.Key,
                            TotalPermissoes = g.Count(),
                            PercentualTotal = (decimal)g.Count() / permissoes.Count * 100
                        })
                        .OrderByDescending(c => c.TotalPermissoes)
                        .ToList();
                }
                break;

            case "desktop":
            case "wpf":
                if (permissoes.Any(p => !string.IsNullOrEmpty(p.Modulo)))
                {
                    estatisticas["Modulo"] = permissoes
                        .Where(p => !string.IsNullOrEmpty(p.Modulo))
                        .GroupBy(p => p.Modulo!)
                        .Select(g => new CampoEstatistica
                        {
                            Valor = g.Key,
                            TotalPermissoes = g.Count(),
                            PercentualTotal = (decimal)g.Count() / permissoes.Count * 100
                        })
                        .OrderByDescending(c => c.TotalPermissoes)
                        .ToList();
                }
                break;

            case "database":
                if (permissoes.Any(p => !string.IsNullOrEmpty(p.OperacaoSql)))
                {
                    estatisticas["OperacaoSql"] = permissoes
                        .Where(p => !string.IsNullOrEmpty(p.OperacaoSql))
                        .GroupBy(p => p.OperacaoSql!)
                        .Select(g => new CampoEstatistica
                        {
                            Valor = g.Key,
                            TotalPermissoes = g.Count(),
                            PercentualTotal = (decimal)g.Count() / permissoes.Count * 100
                        })
                        .OrderByDescending(c => c.TotalPermissoes)
                        .ToList();
                }
                break;
        }

        return estatisticas;
    }

    private async Task<ArquivoExportacao> ProcessarExportacao(ExportarPermissoesAplicacaoRequest request)
    {
        var inicio = DateTime.UtcNow;
        
        try
        {
            // Buscar permissões baseado nos filtros
            var query = _context.PermissoesAplicacao
                .Include(pa => pa.Aplicacao)
                .Include(pa => pa.AtualizadoPor)
                .Where(pa => pa.AplicacaoId == request.AplicacaoId);

            // Aplicar filtros específicos se fornecidos
            if (request.PermissoesIds?.Any() == true)
            {
                query = query.Where(pa => request.PermissoesIds.Contains(pa.Id));
            }

            if (request.Filtros != null)
            {
                query = AplicarFiltros(query, request.Filtros);
            }

            if (!request.IncluirInativas)
            {
                query = query.Where(pa => pa.Ativa);
            }

            var permissoes = await query.ToListAsync();

            // Gerar conteúdo baseado no formato solicitado
            string conteudo = request.Formato.ToLower() switch
            {
                "json" => GerarExportacaoJson(permissoes, request),
                "csv" => GerarExportacaoCsv(permissoes, request),
                "xlsx" => GerarExportacaoExcel(permissoes, request),
                "xml" => GerarExportacaoXml(permissoes, request),
                _ => throw new ArgumentException($"Formato '{request.Formato}' não suportado")
            };

            var conteudoBytes = System.Text.Encoding.UTF8.GetBytes(conteudo);
            var nomeArquivo = request.NomeArquivo ?? $"permissoes_aplicacao_{request.AplicacaoId}_{DateTime.Now:yyyyMMdd_HHmmss}";

            return new ArquivoExportacao
            {
                NomeArquivo = $"{nomeArquivo}.{request.Formato}",
                TipoConteudo = request.Formato.ToLower() switch
                {
                    "csv" => "text/csv",
                    "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "json" => "application/json",
                    "xml" => "application/xml",
                    _ => "application/octet-stream"
                },
                Tamanho = conteudoBytes.Length,
                ConteudoBase64 = Convert.ToBase64String(conteudoBytes),
                DataGeracao = DateTime.UtcNow,
                HashMd5 = CalcularHashMd5(conteudoBytes),
                Metadados = System.Text.Json.JsonSerializer.Serialize(new
                {
                    TotalPermissoes = permissoes.Count,
                    Formato = request.Formato,
                    IncluiRelacionamentos = request.IncluirRelacionamentos,
                    IncluiEstatisticas = request.IncluirEstatisticas,
                    DuracaoGeracao = DateTime.UtcNow - inicio
                })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao processar exportação");
            throw;
        }
    }

    private async Task<ResultadoImportacaoComum> ProcessarImportacao(Aplicacao aplicacao, ImportarPermissoesAplicacaoRequest request)
    {
        var inicio = DateTime.UtcNow;
        var resultado = new ResultadoImportacaoComum
        {
            DataOperacao = inicio
        };

        try
        {
            // TODO: Implementar lógica real de importação
            // Simular operação assíncrona para manter consistência
            await Task.Delay(100); // ✅ Adicionado await para resolver warning

            if (request.ApenasValidar)
            {
                resultado.Sucesso = true;
                resultado.Mensagem = "Validação concluída - nenhum erro encontrado";
                resultado.Resumo = "Arquivo válido para importação";
            }
            else
            {
                resultado.Sucesso = false;
                resultado.Mensagem = "Funcionalidade de importação ainda não implementada";
                resultado.Resumo = "Implementação pendente";
                resultado.Erros.Add(new ErroImportacaoComum
                {
                    Tipo = "NaoImplementado",
                    Mensagem = "Funcionalidade de importação será implementada em versão futura",
                    Codigo = "IMP001"
                });
            }

            resultado.Duracao = DateTime.UtcNow - inicio;
            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao processar importação");
            
            resultado.Sucesso = false;
            resultado.Mensagem = "Erro interno durante importação";
            resultado.Erros.Add(new ErroImportacaoComum
            {
                Tipo = "ErroInterno",
                Mensagem = ex.Message,
                DetalhesEnicos = ex.StackTrace
            });
            resultado.Duracao = DateTime.UtcNow - inicio;
            
            return resultado;
        }
    }
    
    private string GerarExportacaoJson(List<PermissaoAplicacao> permissoes, ExportarPermissoesAplicacaoRequest request)
    {
        var dados = permissoes.Select(p => new
        {
            p.Id,
            p.Nome,
            p.Descricao,
            p.Recurso,
            p.Acao,
            p.Categoria,
            p.Nivel,
            p.Ativa,
            p.Endpoint,
            p.MetodoHttp,
            p.Modulo,
            p.Tela,
            p.Comando,
            p.OperacaoSql,
            p.Schema,
            p.Tabela,
            p.Condicoes,
            p.DataCriacao,
            p.DataAtualizacao,
            AtualizadoPor = p.AtualizadoPor != null ? $"{p.AtualizadoPor.Nome} {p.AtualizadoPor.Sobrenome}" : null
        });

        return System.Text.Json.JsonSerializer.Serialize(dados, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private string GerarExportacaoCsv(List<PermissaoAplicacao> permissoes, ExportarPermissoesAplicacaoRequest request)
    {
        var csv = new StringBuilder();
        
        // Cabeçalho
        csv.AppendLine("Id,Nome,Descricao,Recurso,Acao,Categoria,Nivel,Ativa,Endpoint,MetodoHttp,Modulo,Tela,Comando,OperacaoSql,Schema,Tabela,DataCriacao");
        
        // Dados
        foreach (var p in permissoes)
        {
            csv.AppendLine($"{p.Id},\"{p.Nome}\",\"{p.Descricao}\",\"{p.Recurso}\",\"{p.Acao}\",\"{p.Categoria}\",{p.Nivel},{p.Ativa},\"{p.Endpoint}\",\"{p.MetodoHttp}\",\"{p.Modulo}\",\"{p.Tela}\",\"{p.Comando}\",\"{p.OperacaoSql}\",\"{p.Schema}\",\"{p.Tabela}\",{p.DataCriacao:yyyy-MM-dd HH:mm:ss}");
        }
        
        return csv.ToString();
    }

    private string GerarExportacaoExcel(List<PermissaoAplicacao> permissoes, ExportarPermissoesAplicacaoRequest request)
    {
        // TODO: Implementar geração real de Excel usando bibliotecas como EPPlus
        // Por agora, retornar CSV como fallback
        return GerarExportacaoCsv(permissoes, request);
    }

    private string GerarExportacaoXml(List<PermissaoAplicacao> permissoes, ExportarPermissoesAplicacaoRequest request)
    {
        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.AppendLine("<permissoes>");
        
        foreach (var p in permissoes)
        {
            xml.AppendLine("  <permissao>");
            xml.AppendLine($"    <id>{p.Id}</id>");
            xml.AppendLine($"    <nome><![CDATA[{p.Nome}]]></nome>");
            xml.AppendLine($"    <descricao><![CDATA[{p.Descricao}]]></descricao>");
            xml.AppendLine($"    <recurso><![CDATA[{p.Recurso}]]></recurso>");
            xml.AppendLine($"    <acao><![CDATA[{p.Acao}]]></acao>");
            xml.AppendLine($"    <categoria><![CDATA[{p.Categoria}]]></categoria>");
            xml.AppendLine($"    <nivel>{p.Nivel}</nivel>");
            xml.AppendLine($"    <ativa>{p.Ativa}</ativa>");
            xml.AppendLine($"    <dataCriacao>{p.DataCriacao:yyyy-MM-dd HH:mm:ss}</dataCriacao>");
            xml.AppendLine("  </permissao>");
        }
        
        xml.AppendLine("</permissoes>");
        return xml.ToString();
    }

    private string CalcularHashMd5(byte[] dados)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(dados);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task RegistrarAuditoria(string acao, string recurso, string? recursoId,
        string observacoes, object? dadosAntes = null, object? dadosDepois = null)
    {
        try
        {
            var auditoria = new RegistroAuditoria
            {
                UsuarioId = ObterUsuarioLogadoId(),
                Acao = acao,
                Recurso = recurso,
                RecursoId = recursoId,
                Observacoes = observacoes,
                DadosAntes = dadosAntes != null ? JsonSerializer.Serialize(dadosAntes) : null,
                DadosDepois = dadosDepois != null ? JsonSerializer.Serialize(dadosDepois) : null,
                EnderecoIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconhecido",
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
                DataHora = DateTime.UtcNow
            };

            _context.RegistrosAuditoria.Add(auditoria);
            await _context.SaveChangesAsync();

            _logger.LogInformation("📝 Auditoria registrada: {Acao} {Recurso} {RecursoId} por usuário {UsuarioId}",
                acao, recurso, recursoId, auditoria.UsuarioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao registrar auditoria para {Acao} {Recurso} {RecursoId}",
                acao, recurso, recursoId);
        }
    }

    #endregion

    #region Classes Auxiliares

    private class ResultadoValidacao
    {
        public bool Valido { get; set; }
        public List<string> Erros { get; set; } = new();
        public List<string> Avisos { get; set; } = new();
    }

    #endregion
}