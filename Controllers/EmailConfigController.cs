using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Gestus.Services;
using Gestus.DTOs.Sistema;
using Gestus.Dados;
using Gestus.Modelos;
using Gestus.Extensoes;
using System.ComponentModel.DataAnnotations;

namespace Gestus.Controllers;

/// <summary>
/// Controller para configuração de email do sistema
/// </summary>
[ApiController]
[Route("api/email-config")]
[Authorize]
[Produces("application/json")]
public class EmailConfigController : ControllerBase
{
    private readonly GestusDbContexto _context;
    private readonly IEmailService _emailService;
    private readonly IChaveVersaoService _chaveVersaoService;
    private readonly ILogger<EmailConfigController> _logger;

    public EmailConfigController(
        GestusDbContexto context,
        IEmailService emailService,
        IChaveVersaoService chaveVersaoService,
        ILogger<EmailConfigController> logger)
    {
        _context = context;
        _emailService = emailService;
        _chaveVersaoService = chaveVersaoService;
        _logger = logger;
    }

    /// <summary>
    /// Obter configuração de email ativa
    /// </summary>
    /// <returns>Configuração de email (sem senha)</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ConfiguracaoEmailResponse), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ConfiguracaoEmailResponse>> ObterConfiguracao()
    {
        try
        {
            // ✅ ADICIONAR: Debug das claims para descobrir o problema
            _logger.LogInformation("🔍 === DEBUG: Claims do usuário ===");
            foreach (var claim in User.Claims)
            {
                _logger.LogInformation($"   Tipo: {claim.Type} | Valor: {claim.Value}");
            }
            _logger.LogInformation("🔍 === FIM DEBUG CLAIMS ===");

            // ✅ USAR EXTENSÃO COM BYPASS SUPER ADMIN
            if (!User.TemPermissao("Email", "Listar") && !User.TemPermissao("Email", "Visualizar"))
            {
                _logger.LogWarning("🚫 Acesso negado ao endpoint ObterConfiguracao para usuário {UserId}", 
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                
                return StatusCode(403, new { message = "Usuário não tem permissão para visualizar configurações de email" });
            }

            var configuracao = await _context.Set<ConfiguracaoEmail>()
                .Include(c => c.Templates)
                .Where(c => c.Ativo)
                .FirstOrDefaultAsync();

            if (configuracao == null)
            {
                return NotFound(new { message = "Nenhuma configuração de email encontrada" });
            }

            var response = new ConfiguracaoEmailResponse
            {
                Id = configuracao.Id,
                ServidorSmtp = configuracao.ServidorSmtp,
                Porta = configuracao.Porta,
                EmailRemetente = configuracao.EmailRemetente,
                NomeRemetente = configuracao.NomeRemetente,
                UsarSsl = configuracao.UsarSsl,
                UsarTls = configuracao.UsarTls,
                UsarAutenticacao = configuracao.UsarAutenticacao,
                EmailResposta = configuracao.EmailResposta,
                EmailCopia = configuracao.EmailCopia,
                // ✅ ADICIONAR: A propriedade que estava faltando no DTO
                Ativo = configuracao.Ativo,
                DataCriacao = configuracao.DataCriacao,
                DataAtualizacao = configuracao.DataAtualizacao,
                Templates = configuracao.Templates.Select(t => new TemplateEmailResponse
                {
                    Id = t.Id,
                    Tipo = t.Tipo,
                    Assunto = t.Assunto,
                    CorpoHtml = t.CorpoHtml,
                    CorpoTexto = t.CorpoTexto,
                    Ativo = t.Ativo
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter configuração de email");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Criar ou atualizar configuração de email
    /// </summary>
    /// <param name="request">Dados da configuração</param>
    /// <returns>Configuração criada/atualizada</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ConfiguracaoEmailResponse), 200)]
    [ProducesResponseType(typeof(ConfiguracaoEmailResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<ConfiguracaoEmailResponse>> ConfigurarEmail([FromBody] ConfigurarEmailRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // ✅ CORRIGIDO: Verificar permissão corretamente
            if (!User.TemPermissao("Email", "Configurar"))
            {
                return StatusCode(403, new { message = "Usuário não tem permissão para configurar email do sistema" });
            }

            var usuarioId = ObterUsuarioId();
            if (usuarioId == 0)
            {
                return Unauthorized(new { message = "Token de autenticação inválido" });
            }

            // Encriptar senha
            var senhaEncriptada = await _chaveVersaoService.EncriptarComVersaoAsync(request.Senha, "EmailPassword");

            // Verificar se já existe configuração ativa
            var configuracaoExistente = await _context.Set<ConfiguracaoEmail>()
                .FirstOrDefaultAsync(c => c.Ativo);

            ConfiguracaoEmail configuracao;
            bool isUpdate = false;

            if (configuracaoExistente != null)
            {
                // Atualizar configuração existente
                configuracaoExistente.ServidorSmtp = request.ServidorSmtp;
                configuracaoExistente.Porta = request.Porta;
                configuracaoExistente.EmailRemetente = request.EmailRemetente;
                configuracaoExistente.NomeRemetente = request.NomeRemetente;
                configuracaoExistente.SenhaEncriptada = senhaEncriptada;
                configuracaoExistente.UsarSsl = request.UsarSsl;
                configuracaoExistente.UsarTls = request.UsarTls;
                configuracaoExistente.UsarAutenticacao = request.UsarAutenticacao;
                configuracaoExistente.EmailResposta = request.EmailResposta;
                configuracaoExistente.EmailCopia = request.EmailCopia;
                configuracaoExistente.DataAtualizacao = DateTime.UtcNow;

                configuracao = configuracaoExistente;
                isUpdate = true;

                _logger.LogInformation("📧 Configuração de email atualizada - ID: {Id}, Usuário: {UsuarioId}", 
                    configuracao.Id, usuarioId);
            }
            else
            {
                // Criar nova configuração
                configuracao = new ConfiguracaoEmail
                {
                    ServidorSmtp = request.ServidorSmtp,
                    Porta = request.Porta,
                    EmailRemetente = request.EmailRemetente,
                    NomeRemetente = request.NomeRemetente,
                    SenhaEncriptada = senhaEncriptada,
                    UsarSsl = request.UsarSsl,
                    UsarTls = request.UsarTls,
                    UsarAutenticacao = request.UsarAutenticacao,
                    EmailResposta = request.EmailResposta,
                    EmailCopia = request.EmailCopia,
                    Ativo = true
                };

                _context.Set<ConfiguracaoEmail>().Add(configuracao);

                _logger.LogInformation("📧 Nova configuração de email criada - Usuário: {UsuarioId}", usuarioId);
            }

            await _context.SaveChangesAsync();

            // Recarregar com templates
            var configuracaoRecarregada = await _context.Set<ConfiguracaoEmail>()
                .Include(c => c.Templates)
                .FirstOrDefaultAsync(c => c.Id == configuracao.Id);

            if (configuracaoRecarregada == null)
            {
                return StatusCode(500, new { message = "Erro ao recarregar configuração" });
            }

            var response = new ConfiguracaoEmailResponse
            {
                Id = configuracaoRecarregada.Id,
                ServidorSmtp = configuracaoRecarregada.ServidorSmtp,
                Porta = configuracaoRecarregada.Porta,
                EmailRemetente = configuracaoRecarregada.EmailRemetente,
                NomeRemetente = configuracaoRecarregada.NomeRemetente,
                UsarSsl = configuracaoRecarregada.UsarSsl,
                UsarTls = configuracaoRecarregada.UsarTls,
                UsarAutenticacao = configuracaoRecarregada.UsarAutenticacao,
                EmailResposta = configuracaoRecarregada.EmailResposta,
                EmailCopia = configuracaoRecarregada.EmailCopia,
                Ativo = configuracaoRecarregada.Ativo,
                DataCriacao = configuracaoRecarregada.DataCriacao,
                DataAtualizacao = configuracaoRecarregada.DataAtualizacao,
                Templates = configuracaoRecarregada.Templates.Select(t => new TemplateEmailResponse
                {
                    Id = t.Id,
                    Tipo = t.Tipo,
                    Assunto = t.Assunto,
                    CorpoHtml = t.CorpoHtml,
                    CorpoTexto = t.CorpoTexto,
                    Ativo = t.Ativo
                }).ToList()
            };

            if (isUpdate)
            {
                return Ok(response);
            }
            else
            {
                return CreatedAtAction(nameof(ObterConfiguracao), response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao configurar email - Servidor: {Servidor}", request.ServidorSmtp);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Testar configuração de email
    /// </summary>
    /// <param name="request">Dados para teste</param>
    /// <returns>Resultado do teste</returns>
    [HttpPost("testar")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> TestarConfiguracao([FromBody] TesteEmailRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // ✅ CORRIGIR: Usar extensão
            if (!User.TemPermissao("Email", "Testar") && !User.TemQualquerPermissao("sistema.email.testar"))
            {
                return StatusCode(403, new { message = "Usuário não tem permissão para testar configurações de email" });
            }

            var sucesso = await _emailService.TestarConfiguracaoAsync(request.EmailDestino);

            var usuarioId = ObterUsuarioId();
            _logger.LogInformation("📧 Teste de email - Email: {Email}, Sucesso: {Sucesso}, Usuário: {UsuarioId}", 
                request.EmailDestino, sucesso, usuarioId);

            return Ok(new
            {
                sucesso = sucesso,
                emailDestino = request.EmailDestino,
                dataHoraTeste = DateTime.UtcNow,
                mensagem = sucesso 
                    ? "Email de teste enviado com sucesso!" 
                    : "Falha ao enviar email de teste. Verifique as configurações."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao testar configuração de email - Email: {Email}", request.EmailDestino);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Testar configuração específica (sem salvar)
    /// </summary>
    /// <param name="request">Configuração para testar</param>
    /// <returns>Resultado do teste</returns>
    [HttpPost("testar-configuracao")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> TestarConfiguracaoEspecifica([FromBody] TestarConfiguracaoEmailRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // ✅ CORRIGIR: Usar extensão
            if (!User.TemPermissao("Email", "Testar") && !User.TemQualquerPermissao("sistema.email.testar"))
            {
                return StatusCode(403, new { message = "Usuário não tem permissão para testar configurações de email" });
            }

            var sucesso = await TestarConfiguracaoEmailDiretamente(request);

            var usuarioId = ObterUsuarioId();
            _logger.LogInformation("📧 Teste de configuração específica - Servidor: {Servidor}, Email: {Email}, Sucesso: {Sucesso}, Usuário: {UsuarioId}", 
                request.ServidorSmtp, request.EmailDestino, sucesso, usuarioId);

            return Ok(new
            {
                sucesso = sucesso,
                servidor = request.ServidorSmtp,
                porta = request.Porta,
                emailDestino = request.EmailDestino,
                dataHoraTeste = DateTime.UtcNow,
                mensagem = sucesso 
                    ? "Configuração testada com sucesso!" 
                    : "Falha no teste. Verifique os dados informados."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao testar configuração específica - Servidor: {Servidor}", request.ServidorSmtp);
            return Ok(new
            {
                sucesso = false,
                servidor = request.ServidorSmtp,
                porta = request.Porta,
                emailDestino = request.EmailDestino,
                dataHoraTeste = DateTime.UtcNow,
                mensagem = $"Erro no teste: {ex.Message}",
                detalhesErro = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Desativar configuração de email
    /// </summary>
    /// <returns>Confirmação da desativação</returns>
    [HttpDelete]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> DesativarConfiguracao()
    {
        try
        {
            // ✅ CORRIGIR: Usar extensão (já está correto, manter)
            if (!User.TemPermissao("Email", "Configurar"))
            {
                return StatusCode(403, new { message = "Usuário não tem permissão para desativar configurações de email" });
            }

            var configuracao = await _context.Set<ConfiguracaoEmail>()
                .FirstOrDefaultAsync(c => c.Ativo);

            if (configuracao == null)
            {
                return NotFound(new { message = "Nenhuma configuração ativa encontrada" });
            }

            configuracao.Ativo = false;
            configuracao.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var usuarioId = ObterUsuarioId();
            _logger.LogInformation("📧 Configuração de email desativada - ID: {Id}, Usuário: {UsuarioId}", 
                configuracao.Id, usuarioId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao desativar configuração");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obter status da configuração de email
    /// </summary>
    /// <returns>Status da configuração</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> ObterStatus()
    {
        try
        {
            // ✅ CORRIGIR: Trocar verificação antiga por extensão
            if (!User.TemPermissao("Email", "Visualizar") && !User.TemQualquerPermissao("sistema.email.ler"))
            {
                return StatusCode(403, new { message = "Usuário não tem permissão para visualizar status de email" });
            }

            var configuracao = await _context.Set<ConfiguracaoEmail>()
                .Where(c => c.Ativo)
                .FirstOrDefaultAsync();

            var status = new
            {
                configuracaoAtiva = configuracao != null,
                configId = configuracao?.Id,
                servidor = configuracao?.ServidorSmtp,
                porta = configuracao?.Porta,
                remetente = configuracao?.EmailRemetente,
                ultimaAtualizacao = configuracao?.DataAtualizacao ?? configuracao?.DataCriacao,
                quantidadeTemplates = configuracao?.Templates?.Count ?? 0
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter status da configuração de email");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obter histórico de configurações
    /// </summary>
    /// <returns>Lista de configurações anteriores</returns>
    [HttpGet("historico")]
    [ProducesResponseType(typeof(List<object>), 200)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> ObterHistorico()
    {
        try
        {
            // ✅ CORRIGIR: Trocar verificação antiga por extensão
            if (!User.TemPermissao("Email", "Visualizar") && !User.TemQualquerPermissao("sistema.email.ler"))
            {
                return StatusCode(403, new { message = "Usuário não tem permissão para visualizar histórico de configurações" });
            }

            var configuracoes = await _context.Set<ConfiguracaoEmail>()
                .OrderByDescending(c => c.DataCriacao)
                .Select(c => new
                {
                    c.Id,
                    c.ServidorSmtp,
                    c.Porta,
                    c.EmailRemetente,
                    c.NomeRemetente,
                    c.Ativo,
                    c.DataCriacao,
                    c.DataAtualizacao
                })
                .ToListAsync();

            return Ok(configuracoes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter histórico de configurações");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    // ✅ MÉTODOS AUXILIARES PRIVADOS

    private int ObterUsuarioId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private async Task<bool> TestarConfiguracaoEmailDiretamente(TestarConfiguracaoEmailRequest request)
    {
        try
        {
            using var client = new System.Net.Mail.SmtpClient(request.ServidorSmtp, request.Porta);
            
            if (request.UsarAutenticacao)
            {
                client.Credentials = new System.Net.NetworkCredential(request.EmailRemetente, request.Senha);
            }

            client.EnableSsl = request.UsarSsl;
            
            var message = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(request.EmailRemetente, request.NomeRemetente),
                Subject = $"[TESTE] Configuração de Email - {DateTime.Now:dd/MM/yyyy HH:mm}",
                Body = $@"
                    <h2>🎯 Teste de Configuração de Email</h2>
                    <p><strong>Sistema:</strong> Gestus IAM</p>
                    <p><strong>Servidor SMTP:</strong> {request.ServidorSmtp}:{request.Porta}</p>
                    <p><strong>Data/Hora:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
                    <p><strong>Status:</strong> ✅ Configuração testada com sucesso!</p>
                    <hr>
                    <p><em>Este é um email de teste para validar a configuração antes de salvar.</em></p>
                ",
                IsBodyHtml = true
            };

            message.To.Add(request.EmailDestino);

            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ Falha no teste direto de email: {Erro}", ex.Message);
            return false;
        }
    }
}

/// <summary>
/// ✅ REQUEST PARA TESTAR CONFIGURAÇÃO ESPECÍFICA
/// </summary>
public class TestarConfiguracaoEmailRequest
{
    [Required(ErrorMessage = "Servidor SMTP é obrigatório")]
    public string ServidorSmtp { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Porta é obrigatória")]
    [Range(1, 65535, ErrorMessage = "Porta deve estar entre 1 e 65535")]
    public int Porta { get; set; }
    
    [Required(ErrorMessage = "Email do remetente é obrigatório")]
    [EmailAddress(ErrorMessage = "Email do remetente deve ter formato válido")]
    public string EmailRemetente { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Nome do remetente é obrigatório")]
    public string NomeRemetente { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Senha é obrigatória")]
    public string Senha { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email de destino é obrigatório")]
    [EmailAddress(ErrorMessage = "Email de destino deve ter formato válido")]
    public string EmailDestino { get; set; } = string.Empty;
    
    public bool UsarSsl { get; set; } = true;
    public bool UsarTls { get; set; } = true;
    public bool UsarAutenticacao { get; set; } = true;
}