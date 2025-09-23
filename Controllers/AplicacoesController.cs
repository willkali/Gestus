using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Gestus.Modelos;
using Gestus.Dados;
using Gestus.DTOs.Aplicacao;
using Gestus.DTOs.Comuns;
using System.Text.Json;

namespace Gestus.Controllers;

/// <summary>
/// Controller para gerenciamento de aplicações do sistema
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class AplicacoesController : ControllerBase
{
    private readonly GestusDbContexto _context;
    private readonly ILogger<AplicacoesController> _logger;

    public AplicacoesController(
        GestusDbContexto context,
        ILogger<AplicacoesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista aplicações com filtros e paginação
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(RespostaPaginada<AplicacaoResumo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarAplicacoes([FromQuery] FiltrosAplicacao filtros)
    {
        try
        {
            if (!TemPermissao("Aplicacoes.Listar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para listar aplicações" 
                });
            }

            var query = _context.Aplicacoes
                .Include(a => a.TipoAplicacao)
                .Include(a => a.StatusAplicacao)
                .Include(a => a.CriadoPor)
                .AsQueryable();

            // Aplicar filtros
            query = AplicarFiltros(query, filtros);

            // Aplicar ordenação
            query = AplicarOrdenacao(query, filtros.OrdenarPor, filtros.DirecaoOrdenacao);

            // Paginação
            var totalItens = await query.CountAsync();
            var aplicacoes = await query
                .Skip((filtros.Pagina - 1) * filtros.ItensPorPagina)
                .Take(filtros.ItensPorPagina)
                .Select(a => new AplicacaoResumo
                {
                    Id = a.Id,
                    Nome = a.Nome,
                    Codigo = a.Codigo,
                    Descricao = a.Descricao,
                    UrlBase = a.UrlBase,
                    TipoAplicacao = new TipoAplicacaoResumo
                    {
                        Id = a.TipoAplicacao.Id,
                        Nome = a.TipoAplicacao.Nome,
                        Codigo = a.TipoAplicacao.Codigo,
                        Icone = a.TipoAplicacao.Icone,
                        Cor = a.TipoAplicacao.Cor
                    },
                    StatusAplicacao = new StatusAplicacaoResumo
                    {
                        Id = a.StatusAplicacao.Id,
                        Nome = a.StatusAplicacao.Nome,
                        Codigo = a.StatusAplicacao.Codigo,
                        CorFundo = a.StatusAplicacao.CorFundo,
                        CorTexto = a.StatusAplicacao.CorTexto,
                        Icone = a.StatusAplicacao.Icone
                    },
                    Versao = a.Versao,
                    NivelSeguranca = a.NivelSeguranca,
                    Ativa = a.Ativa,
                    TotalUsuarios = a.UsuariosAplicacao.Count(ua => ua.Ativo && ua.Aprovado),
                    TotalPermissoes = a.Permissoes.Count(p => p.Ativa),
                    DataCriacao = a.DataCriacao,
                    CriadoPor = a.CriadoPor.Nome + " " + a.CriadoPor.Sobrenome
                })
                .ToListAsync();

            var resposta = new RespostaPaginada<AplicacaoResumo>
            {
                Dados = aplicacoes,
                TotalItens = totalItens,
                PaginaAtual = filtros.Pagina,
                ItensPorPagina = filtros.ItensPorPagina,
                TotalPaginas = (int)Math.Ceiling((double)totalItens / filtros.ItensPorPagina)
            };

            return Ok(resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar aplicações");
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Obtém detalhes de uma aplicação específica
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AplicacaoCompleta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterAplicacao(int id)
    {
        try
        {
            if (!TemPermissao("Aplicacoes.Visualizar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para visualizar aplicações" 
                });
            }

            var aplicacao = await _context.Aplicacoes
                .Include(a => a.TipoAplicacao)
                .Include(a => a.StatusAplicacao)
                .Include(a => a.CriadoPor)
                .Include(a => a.AtualizadoPor)
                .Include(a => a.Permissoes)
                .Include(a => a.UsuariosAplicacao)
                    .ThenInclude(ua => ua.Usuario)
                .Include(a => a.HistoricoStatus)
                    .ThenInclude(h => h.StatusNovo)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (aplicacao == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "AplicacaoNaoEncontrada", 
                    Mensagem = "Aplicação não encontrada" 
                });
            }

            var aplicacaoCompleta = ConstruirAplicacaoCompleta(aplicacao);

            await RegistrarAuditoria("Visualizar", "Aplicacao", id.ToString(), 
                $"Visualização da aplicação '{aplicacao.Nome}'");

            return Ok(aplicacaoCompleta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter aplicação {Id}", id);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Cria nova aplicação
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AplicacaoCompleta), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CriarAplicacao([FromBody] CriarAplicacaoRequest request)
    {
        try
        {
            if (!TemPermissao("Aplicacoes.Criar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para criar aplicações" 
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verificar se código já existe
            var codigoExiste = await _context.Aplicacoes
                .AnyAsync(a => a.Codigo == request.Codigo);

            if (codigoExiste)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "CodigoDuplicado", 
                    Mensagem = $"Já existe uma aplicação com o código '{request.Codigo}'" 
                });
            }

            // Verificar se ClientId já existe (se fornecido)
            if (!string.IsNullOrEmpty(request.ClientId))
            {
                var clientIdExiste = await _context.Aplicacoes
                    .AnyAsync(a => a.ClientId == request.ClientId);

                if (clientIdExiste)
                {
                    return BadRequest(new RespostaErro 
                    { 
                        Erro = "ClientIdDuplicado", 
                        Mensagem = $"Já existe uma aplicação com o ClientId '{request.ClientId}'" 
                    });
                }
            }

            var usuarioId = ObterUsuarioLogadoId();
            var aplicacao = new Aplicacao
            {
                Nome = request.Nome,
                Codigo = request.Codigo,
                Descricao = request.Descricao,
                UrlBase = request.UrlBase,
                TipoAplicacaoId = request.TipoAplicacaoId,
                StatusAplicacaoId = request.StatusAplicacaoId ?? 1, // Default: Ativa
                Versao = request.Versao ?? "1.0.0",
                ClientId = request.ClientId,
                ClientSecretEncriptado = request.ClientSecretEncriptado,
                UrlsRedirecionamento = request.UrlsRedirecionamento ?? "[]",
                ScopesPermitidos = request.ScopesPermitidos ?? "[]",
                Configuracoes = request.Configuracoes ?? "{}",
                MetadadosTipo = request.MetadadosTipo ?? "{}",
                NivelSeguranca = request.NivelSeguranca,
                RequerAprovacao = request.RequerAprovacao,
                PermiteAutoRegistro = request.PermiteAutoRegistro,
                Observacoes = request.Observacoes,
                CriadoPorId = usuarioId,
                DataCriacao = DateTime.UtcNow
            };

            _context.Aplicacoes.Add(aplicacao);
            await _context.SaveChangesAsync();

            // Recarregar com dados relacionados
            var aplicacaoCompleta = await ObterAplicacaoCompletaAsync(aplicacao.Id);

            await RegistrarAuditoria("Criar", "Aplicacao", aplicacao.Id.ToString(), 
                $"Aplicação '{aplicacao.Nome}' criada", null, aplicacao);

            _logger.LogInformation("✅ Aplicação criada - ID: {Id}, Nome: {Nome}, Usuário: {UsuarioId}", 
                aplicacao.Id, aplicacao.Nome, usuarioId);

            return CreatedAtAction(nameof(ObterAplicacao), new { id = aplicacao.Id }, aplicacaoCompleta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao criar aplicação - Nome: {Nome}", request.Nome);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Atualiza aplicação existente
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AplicacaoCompleta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AtualizarAplicacao(int id, [FromBody] AtualizarAplicacaoRequest request)
    {
        try
        {
            if (!TemPermissao("Aplicacoes.Editar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para editar aplicações" 
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var aplicacao = await _context.Aplicacoes.FindAsync(id);
            if (aplicacao == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "AplicacaoNaoEncontrada", 
                    Mensagem = "Aplicação não encontrada" 
                });
            }

            var dadosAntes = JsonSerializer.Serialize(aplicacao);
            var usuarioId = ObterUsuarioLogadoId();

            // Atualizar campos
            if (!string.IsNullOrEmpty(request.Nome))
                aplicacao.Nome = request.Nome;
            
            if (!string.IsNullOrEmpty(request.Descricao))
                aplicacao.Descricao = request.Descricao;
            
            if (!string.IsNullOrEmpty(request.UrlBase))
                aplicacao.UrlBase = request.UrlBase;
            
            if (request.TipoAplicacaoId.HasValue)
                aplicacao.TipoAplicacaoId = request.TipoAplicacaoId.Value;
            
            if (request.StatusAplicacaoId.HasValue)
            {
                var statusAnterior = aplicacao.StatusAplicacaoId;
                aplicacao.StatusAplicacaoId = request.StatusAplicacaoId.Value;
                
                // Registrar mudança de status no histórico
                if (statusAnterior != request.StatusAplicacaoId.Value)
                {
                    var historicoStatus = new HistoricoStatusAplicacao
                    {
                        AplicacaoId = aplicacao.Id,
                        StatusAnteriorId = statusAnterior,
                        StatusNovoId = request.StatusAplicacaoId.Value,
                        AlteradoPorId = usuarioId,
                        Motivo = request.MotivoMudancaStatus ?? "Atualização via API",
                        Observacoes = request.ObservacoesMudancaStatus,
                        DataMudanca = DateTime.UtcNow
                    };
                    
                    _context.HistoricoStatusAplicacao.Add(historicoStatus);
                }
            }

            if (!string.IsNullOrEmpty(request.Versao))
                aplicacao.Versao = request.Versao;

            if (request.NivelSeguranca.HasValue)
                aplicacao.NivelSeguranca = request.NivelSeguranca.Value;

            if (request.Ativa.HasValue)
                aplicacao.Ativa = request.Ativa.Value;

            if (request.RequerAprovacao.HasValue)
                aplicacao.RequerAprovacao = request.RequerAprovacao.Value;

            if (request.PermiteAutoRegistro.HasValue)
                aplicacao.PermiteAutoRegistro = request.PermiteAutoRegistro.Value;

            if (!string.IsNullOrEmpty(request.Observacoes))
                aplicacao.Observacoes = request.Observacoes;

            aplicacao.AtualizadoPorId = usuarioId;
            aplicacao.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var dadosDepois = JsonSerializer.Serialize(aplicacao);
            var aplicacaoCompleta = await ObterAplicacaoCompletaAsync(aplicacao.Id);

            await RegistrarAuditoria("Atualizar", "Aplicacao", id.ToString(), 
                $"Aplicação '{aplicacao.Nome}' atualizada", dadosAntes, dadosDepois);

            _logger.LogInformation("✅ Aplicação atualizada - ID: {Id}, Nome: {Nome}, Usuário: {UsuarioId}", 
                aplicacao.Id, aplicacao.Nome, usuarioId);

            return Ok(aplicacaoCompleta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao atualizar aplicação {Id}", id);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Lista tipos de aplicação disponíveis
    /// </summary>
    [HttpGet("tipos")]
    [ProducesResponseType(typeof(List<TipoAplicacaoCompleto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarTiposAplicacao()
    {
        try
        {
            var tipos = await _context.TiposAplicacao
                .Where(t => t.Ativo)
                .OrderBy(t => t.Ordem)
                .ThenBy(t => t.Nome)
                .Select(t => new TipoAplicacaoCompleto
                {
                    Id = t.Id,
                    Codigo = t.Codigo,
                    Nome = t.Nome,
                    Descricao = t.Descricao,
                    Icone = t.Icone,
                    Cor = t.Cor,
                    NivelComplexidade = t.NivelComplexidade,
                    InstrucoesIntegracao = t.InstrucoesIntegracao,
                    CamposPermissao = t.CamposPermissao,
                    ConfiguracoesTipo = t.ConfiguracoesTipo
                })
                .ToListAsync();

            return Ok(tipos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar tipos de aplicação");
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Lista status de aplicação disponíveis
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(List<StatusAplicacaoCompleto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarStatusAplicacao()
    {
        try
        {
            var status = await _context.StatusAplicacao
                .Where(s => s.Ativo)
                .OrderBy(s => s.Ordem)
                .ThenBy(s => s.Nome)
                .Select(s => new StatusAplicacaoCompleto
                {
                    Id = s.Id,
                    Codigo = s.Codigo,
                    Nome = s.Nome,
                    Descricao = s.Descricao,
                    CorFundo = s.CorFundo,
                    CorTexto = s.CorTexto,
                    Icone = s.Icone,
                    PermiteAcesso = s.PermiteAcesso,
                    PermiteNovoUsuario = s.PermiteNovoUsuario,
                    VisivelParaUsuarios = s.VisivelParaUsuarios,
                    MensagemUsuario = s.MensagemUsuario,
                    Prioridade = s.Prioridade
                })
                .ToListAsync();

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar status de aplicação");
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

    private IQueryable<Aplicacao> AplicarFiltros(IQueryable<Aplicacao> query, FiltrosAplicacao filtros)
    {
        if (!string.IsNullOrEmpty(filtros.Nome))
        {
            query = query.Where(a => a.Nome.ToLower().Contains(filtros.Nome.ToLower()));
        }

        if (!string.IsNullOrEmpty(filtros.Codigo))
        {
            query = query.Where(a => a.Codigo.ToLower().Contains(filtros.Codigo.ToLower()));
        }

        if (filtros.TipoAplicacaoId.HasValue)
        {
            query = query.Where(a => a.TipoAplicacaoId == filtros.TipoAplicacaoId.Value);
        }

        if (filtros.StatusAplicacaoId.HasValue)
        {
            query = query.Where(a => a.StatusAplicacaoId == filtros.StatusAplicacaoId.Value);
        }

        if (filtros.Ativa.HasValue)
        {
            query = query.Where(a => a.Ativa == filtros.Ativa.Value);
        }

        if (filtros.NivelSegurancaMinimo.HasValue)
        {
            query = query.Where(a => a.NivelSeguranca >= filtros.NivelSegurancaMinimo.Value);
        }

        if (filtros.NivelSegurancaMaximo.HasValue)
        {
            query = query.Where(a => a.NivelSeguranca <= filtros.NivelSegurancaMaximo.Value);
        }

        return query;
    }

    private IQueryable<Aplicacao> AplicarOrdenacao(IQueryable<Aplicacao> query, string? ordenarPor, string? direcao)
    {
        var desc = direcao?.ToLower() == "desc";

        return ordenarPor?.ToLower() switch
        {
            "nome" => desc ? query.OrderByDescending(a => a.Nome) : query.OrderBy(a => a.Nome),
            "codigo" => desc ? query.OrderByDescending(a => a.Codigo) : query.OrderBy(a => a.Codigo),
            "datacriacao" => desc ? query.OrderByDescending(a => a.DataCriacao) : query.OrderBy(a => a.DataCriacao),
            "nivelseguranca" => desc ? query.OrderByDescending(a => a.NivelSeguranca) : query.OrderBy(a => a.NivelSeguranca),
            _ => query.OrderBy(a => a.Nome)
        };
    }

    private AplicacaoCompleta ConstruirAplicacaoCompleta(Aplicacao aplicacao)
    {
        return new AplicacaoCompleta
        {
            Id = aplicacao.Id,
            Nome = aplicacao.Nome,
            Codigo = aplicacao.Codigo,
            Descricao = aplicacao.Descricao,
            UrlBase = aplicacao.UrlBase,
            TipoAplicacao = new TipoAplicacaoCompleto
            {
                Id = aplicacao.TipoAplicacao.Id,
                Codigo = aplicacao.TipoAplicacao.Codigo,
                Nome = aplicacao.TipoAplicacao.Nome,
                Descricao = aplicacao.TipoAplicacao.Descricao,
                Icone = aplicacao.TipoAplicacao.Icone,
                Cor = aplicacao.TipoAplicacao.Cor,
                NivelComplexidade = aplicacao.TipoAplicacao.NivelComplexidade,
                InstrucoesIntegracao = aplicacao.TipoAplicacao.InstrucoesIntegracao,
                CamposPermissao = aplicacao.TipoAplicacao.CamposPermissao,
                ConfiguracoesTipo = aplicacao.TipoAplicacao.ConfiguracoesTipo
            },
            StatusAplicacao = new StatusAplicacaoCompleto
            {
                Id = aplicacao.StatusAplicacao.Id,
                Codigo = aplicacao.StatusAplicacao.Codigo,
                Nome = aplicacao.StatusAplicacao.Nome,
                Descricao = aplicacao.StatusAplicacao.Descricao,
                CorFundo = aplicacao.StatusAplicacao.CorFundo,
                CorTexto = aplicacao.StatusAplicacao.CorTexto,
                Icone = aplicacao.StatusAplicacao.Icone,
                PermiteAcesso = aplicacao.StatusAplicacao.PermiteAcesso,
                PermiteNovoUsuario = aplicacao.StatusAplicacao.PermiteNovoUsuario,
                VisivelParaUsuarios = aplicacao.StatusAplicacao.VisivelParaUsuarios,
                MensagemUsuario = aplicacao.StatusAplicacao.MensagemUsuario,
                Prioridade = aplicacao.StatusAplicacao.Prioridade
            },
            Versao = aplicacao.Versao,
            ClientId = aplicacao.ClientId,
            UrlsRedirecionamento = aplicacao.UrlsRedirecionamento,
            ScopesPermitidos = aplicacao.ScopesPermitidos,
            Configuracoes = aplicacao.Configuracoes,
            MetadadosTipo = aplicacao.MetadadosTipo,
            NivelSeguranca = aplicacao.NivelSeguranca,
            Ativa = aplicacao.Ativa,
            RequerAprovacao = aplicacao.RequerAprovacao,
            PermiteAutoRegistro = aplicacao.PermiteAutoRegistro,
            Observacoes = aplicacao.Observacoes,
            TotalUsuarios = aplicacao.UsuariosAplicacao.Count(ua => ua.Ativo && ua.Aprovado),
            TotalPermissoes = aplicacao.Permissoes.Count(p => p.Ativa),
            DataCriacao = aplicacao.DataCriacao,
            DataAtualizacao = aplicacao.DataAtualizacao,
            CriadoPor = aplicacao.CriadoPor.Nome + " " + aplicacao.CriadoPor.Sobrenome,
            AtualizadoPor = aplicacao.AtualizadoPor?.Nome + " " + aplicacao.AtualizadoPor?.Sobrenome
        };
    }

    private async Task<AplicacaoCompleta?> ObterAplicacaoCompletaAsync(int aplicacaoId)
    {
        var aplicacao = await _context.Aplicacoes
            .Include(a => a.TipoAplicacao)
            .Include(a => a.StatusAplicacao)
            .Include(a => a.CriadoPor)
            .Include(a => a.AtualizadoPor)
            .Include(a => a.Permissoes)
            .Include(a => a.UsuariosAplicacao)
            .FirstOrDefaultAsync(a => a.Id == aplicacaoId);

        return aplicacao != null ? ConstruirAplicacaoCompleta(aplicacao) : null;
    }

    private async Task RegistrarAuditoria(string acao, string recurso, string? recursoId, 
        string observacoes, object? dadosAntes = null, object? dadosDepois = null)
    {
        try
        {
            var usuarioId = ObterUsuarioLogadoId();
            var enderecoIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            var registro = new RegistroAuditoria
            {
                UsuarioId = usuarioId > 0 ? usuarioId : null,
                Acao = acao,
                Recurso = recurso,
                RecursoId = recursoId,
                Observacoes = observacoes,
                EnderecoIp = enderecoIp,
                UserAgent = userAgent,
                DadosAntes = dadosAntes != null ? JsonSerializer.Serialize(dadosAntes) : null,
                DadosDepois = dadosDepois != null ? JsonSerializer.Serialize(dadosDepois) : null,
                DataHora = DateTime.UtcNow
            };

            _context.RegistrosAuditoria.Add(registro);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao registrar auditoria");
        }
    }

    #endregion
}