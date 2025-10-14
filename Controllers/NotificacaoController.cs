using Gestus.DTOs.Notificacao;
using Gestus.DTOs.Comuns;
using Gestus.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gestus.Controllers;

/// <summary>
/// Controlador para gerenciamento de notificações
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificacaoController : ControllerBase
{
    private readonly INotificacaoService _notificacaoService;
    private readonly ILogger<NotificacaoController> _logger;

    public NotificacaoController(
        INotificacaoService notificacaoService,
        ILogger<NotificacaoController> logger)
    {
        _notificacaoService = notificacaoService;
        _logger = logger;
    }

    /// <summary>
    /// Obter notificações do usuário atual com filtros e paginação
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<RespostaPaginada<NotificacaoDTO>>> ObterNotificacoes(
        [FromQuery] FiltrarNotificacoesDTO filtros)
    {
        try
        {
            var usuarioId = ObterUsuarioIdAtual();
            if (usuarioId == null)
            {
                return Unauthorized("Usuário não autenticado");
            }

            var resultado = await _notificacaoService.ObterNotificacoesUsuarioAsync(usuarioId.Value, filtros);
            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter notificações do usuário");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obter uma notificação específica
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<NotificacaoDTO>> ObterNotificacao(Guid id)
    {
        try
        {
            var usuarioId = ObterUsuarioIdAtual();
            if (usuarioId == null)
            {
                return Unauthorized("Usuário não autenticado");
            }

            var notificacao = await _notificacaoService.ObterNotificacaoPorIdAsync(id, usuarioId.Value);
            
            if (notificacao == null)
            {
                return NotFound("Notificação não encontrada");
            }

            return Ok(notificacao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter notificação {NotificacaoId}", id);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obter contagem de notificações não lidas do usuário atual
    /// </summary>
    [HttpGet("contador")]
    public async Task<ActionResult<int>> ObterContadorNaoLidas()
    {
        try
        {
            var usuarioId = ObterUsuarioIdAtual();
            if (usuarioId == null)
            {
                return Unauthorized("Usuário não autenticado");
            }

            var contador = await _notificacaoService.ObterContagemNaoLidasAsync(usuarioId.Value);
            return Ok(contador);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter contador de notificações não lidas");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Criar uma nova notificação (apenas para administradores)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<NotificacaoDTO>> CriarNotificacao(CriarNotificacaoDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var notificacao = await _notificacaoService.CriarNotificacaoAsync(dto);
            return CreatedAtAction(nameof(ObterNotificacao), new { id = notificacao.Id }, notificacao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar notificação");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Criar notificações em broadcast para múltiplos usuários (apenas para administradores)
    /// </summary>
    [HttpPost("broadcast")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<List<NotificacaoDTO>>> CriarNotificacaoBroadcast(CriarNotificacaoBroadcastDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var notificacoes = await _notificacaoService.CriarNotificacaoBroadcastAsync(dto);
            return Ok(notificacoes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar notificações broadcast");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Marcar uma notificação como lida
    /// </summary>
    [HttpPut("{id}/marcar-lida")]
    public async Task<ActionResult> MarcarComoLida(Guid id)
    {
        try
        {
            var usuarioId = ObterUsuarioIdAtual();
            if (usuarioId == null)
            {
                return Unauthorized("Usuário não autenticado");
            }

            var sucesso = await _notificacaoService.MarcarComoLidaAsync(id, usuarioId.Value);
            
            if (!sucesso)
            {
                return NotFound("Notificação não encontrada");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar notificação {NotificacaoId} como lida", id);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Marcar todas as notificações do usuário atual como lidas
    /// </summary>
    [HttpPut("marcar-todas-lidas")]
    public async Task<ActionResult<int>> MarcarTodasComoLidas()
    {
        try
        {
            var usuarioId = ObterUsuarioIdAtual();
            if (usuarioId == null)
            {
                return Unauthorized("Usuário não autenticado");
            }

            var quantidade = await _notificacaoService.MarcarTodasComoLidasAsync(usuarioId.Value);
            return Ok(quantidade);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar todas as notificações como lidas");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Excluir uma notificação
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> ExcluirNotificacao(Guid id)
    {
        try
        {
            var usuarioId = ObterUsuarioIdAtual();
            if (usuarioId == null)
            {
                return Unauthorized("Usuário não autenticado");
            }

            var sucesso = await _notificacaoService.ExcluirNotificacaoAsync(id, usuarioId.Value);
            
            if (!sucesso)
            {
                return NotFound("Notificação não encontrada");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir notificação {NotificacaoId}", id);
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Limpar notificações antigas (apenas para administradores)
    /// </summary>
    [HttpDelete("limpeza")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<int>> LimparNotificacoesAntigas([FromQuery] int diasRetencao = 90)
    {
        try
        {
            var quantidade = await _notificacaoService.LimparNotificacoesAntigasAsync(diasRetencao);
            return Ok(quantidade);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao limpar notificações antigas");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Criar notificação de teste (apenas em ambiente de desenvolvimento)
    /// </summary>
    [HttpPost("teste")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<NotificacaoDTO>> CriarNotificacaoTeste()
    {
        try
        {
            var usuarioId = ObterUsuarioIdAtual();
            if (usuarioId == null)
            {
                return Unauthorized("Usuário não autenticado");
            }

            var dto = new CriarNotificacaoDTO
            {
                UsuarioId = usuarioId.Value,
                Titulo = "Notificação de Teste",
                Mensagem = $"Esta é uma notificação de teste criada em {DateTime.Now:dd/MM/yyyy HH:mm}",
                Tipo = "teste",
                Cor = "info",
                Origem = "Sistema",
                Prioridade = 3
            };

            var notificacao = await _notificacaoService.CriarNotificacaoAsync(dto);
            return Ok(notificacao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar notificação de teste");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obter ID do usuário atual a partir dos claims
    /// </summary>
    private int? ObterUsuarioIdAtual()
    {
        var userIdClaim = User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}