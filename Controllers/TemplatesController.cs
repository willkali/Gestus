using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Gestus.Services;
using Gestus.DTOs.Sistema;
using System.ComponentModel.DataAnnotations;

namespace Gestus.Controllers;

/// <summary>
/// Controller para gerenciamento de templates de email
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly IEmailService _emailService;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(
        ITemplateService templateService,
        IEmailService emailService,
        ILogger<TemplatesController> logger)
    {
        _templateService = templateService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Obter tipos de template disponíveis
    /// </summary>
    /// <returns>Lista de tipos de template com variáveis</returns>
    [HttpGet("tipos")]
    [ProducesResponseType(typeof(List<TipoTemplateResponse>), 200)]
    public async Task<ActionResult<List<TipoTemplateResponse>>> ObterTiposTemplate()
    {
        try
        {
            var tipos = await _templateService.ObterTiposTemplateAsync();
            return Ok(tipos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter tipos de template");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Validar template antes de criar/atualizar
    /// </summary>
    /// <param name="request">Dados do template para validação</param>
    /// <returns>Resultado da validação</returns>
    [HttpPost("validar")]
    [ProducesResponseType(typeof(ValidacaoTemplateResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<ValidacaoTemplateResponse>> ValidarTemplate([FromBody] ValidarTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var resultado = await _templateService.ValidarTemplateAsync(request);
            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao validar template - Tipo: {Tipo}", request.Tipo);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Gerar preview do template com dados de exemplo
    /// </summary>
    /// <param name="tipo">Tipo do template</param>
    /// <param name="request">Conteúdo do template</param>
    /// <returns>HTML renderizado com variáveis substituídas</returns>
    [HttpPost("preview/{tipo}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> GerarPreview(string tipo, [FromBody] ValidarTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var previewHtml = await _templateService.GerarPreviewAsync(tipo, request.CorpoHtml);
            var previewTexto = !string.IsNullOrEmpty(request.CorpoTexto) 
                ? await _templateService.GerarPreviewAsync(tipo, request.CorpoTexto) 
                : null;

            return Ok(new
            {
                assunto = await _templateService.GerarPreviewAsync(tipo, request.Assunto),
                corpoHtml = previewHtml,
                corpoTexto = previewTexto,
                dataGeracao = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao gerar preview - Tipo: {Tipo}", tipo);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Listar templates personalizados
    /// </summary>
    /// <param name="tipo">Filtrar por tipo (opcional)</param>
    /// <param name="ativo">Filtrar por status ativo (opcional)</param>
    /// <returns>Lista de templates</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<TemplatePersonalizadoResponse>), 200)]
    public async Task<ActionResult<List<TemplatePersonalizadoResponse>>> ListarTemplates(
        [FromQuery] string? tipo = null,
        [FromQuery] bool? ativo = null)
    {
        try
        {
            var templates = await _templateService.ListarTemplatesAsync(tipo, ativo);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar templates - Tipo: {Tipo}, Ativo: {Ativo}", tipo, ativo);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obter template específico por ID
    /// </summary>
    /// <param name="id">ID do template</param>
    /// <returns>Dados do template</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TemplatePersonalizadoResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TemplatePersonalizadoResponse>> ObterTemplate(int id)
    {
        try
        {
            var templates = await _templateService.ListarTemplatesAsync();
            var template = templates.FirstOrDefault(t => t.Id == id);

            if (template == null)
            {
                return NotFound(new { message = "Template não encontrado" });
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter template - ID: {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Criar novo template personalizado
    /// </summary>
    /// <param name="request">Dados do template</param>
    /// <returns>Template criado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TemplatePersonalizadoResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<TemplatePersonalizadoResponse>> CriarTemplate([FromBody] CriarTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verificar permissão
            if (!User.HasClaim("permission", "templates.criar"))
            {
                return Forbid("Usuário não tem permissão para criar templates");
            }

            var usuarioId = ObterUsuarioId();
            if (usuarioId == 0)
            {
                return Unauthorized();
            }

            var template = await _templateService.CriarTemplateAsync(request, usuarioId);

            _logger.LogInformation("✅ Template criado - ID: {Id}, Nome: {Nome}, Usuário: {UsuarioId}", 
                template.Id, template.Nome, usuarioId);

            return CreatedAtAction(nameof(ObterTemplate), new { id = template.Id }, template);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("⚠️ Erro de validação ao criar template: {Erro}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao criar template - Nome: {Nome}", request.Nome);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Atualizar template existente
    /// </summary>
    /// <param name="id">ID do template</param>
    /// <param name="request">Novos dados do template</param>
    /// <returns>Template atualizado</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TemplatePersonalizadoResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TemplatePersonalizadoResponse>> AtualizarTemplate(
        int id, 
        [FromBody] CriarTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verificar permissão
            if (!User.HasClaim("permission", "templates.editar"))
            {
                return Forbid("Usuário não tem permissão para editar templates");
            }

            var usuarioId = ObterUsuarioId();
            if (usuarioId == 0)
            {
                return Unauthorized();
            }

            var template = await _templateService.AtualizarTemplateAsync(id, request, usuarioId);

            if (template == null)
            {
                return NotFound(new { message = "Template não encontrado" });
            }

            _logger.LogInformation("✅ Template atualizado - ID: {Id}, Nome: {Nome}, Usuário: {UsuarioId}", 
                template.Id, template.Nome, usuarioId);

            return Ok(template);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("⚠️ Erro de validação ao atualizar template: {Erro}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao atualizar template - ID: {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Excluir template (soft delete)
    /// </summary>
    /// <param name="id">ID do template</param>
    /// <returns>Confirmação da exclusão</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> ExcluirTemplate(int id)
    {
        try
        {
            // Verificar permissão
            if (!User.HasClaim("permission", "templates.excluir"))
            {
                return Forbid("Usuário não tem permissão para excluir templates");
            }

            var usuarioId = ObterUsuarioId();
            if (usuarioId == 0)
            {
                return Unauthorized();
            }

            var sucesso = await _templateService.ExcluirTemplateAsync(id, usuarioId);

            if (!sucesso)
            {
                return NotFound(new { message = "Template não encontrado" });
            }

            _logger.LogInformation("✅ Template excluído - ID: {Id}, Usuário: {UsuarioId}", id, usuarioId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao excluir template - ID: {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Testar template enviando email
    /// </summary>
    /// <param name="id">ID do template</param>
    /// <param name="request">Dados para teste</param>
    /// <returns>Resultado do teste</returns>
    [HttpPost("{id:int}/testar")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> TestarTemplate(int id, [FromBody] TesteEmailRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verificar permissão
            if (!User.HasClaim("permission", "templates.testar"))
            {
                return Forbid("Usuário não tem permissão para testar templates");
            }

            // Obter template
            var templates = await _templateService.ListarTemplatesAsync();
            var template = templates.FirstOrDefault(t => t.Id == id);

            if (template == null)
            {
                return NotFound(new { message = "Template não encontrado" });
            }

            // Preparar variáveis para teste
            var variaveis = request.VariaveisTemplate ?? new Dictionary<string, string>();
            
            // Adicionar variáveis padrão se não fornecidas
            if (!variaveis.ContainsKey("NomeUsuario"))
                variaveis["NomeUsuario"] = "Usuário Teste";
            if (!variaveis.ContainsKey("EmailUsuario"))
                variaveis["EmailUsuario"] = request.EmailDestino;
            if (!variaveis.ContainsKey("NomeSistema"))
                variaveis["NomeSistema"] = "Gestus";

            // Gerar conteúdo do email
            var assunto = await _templateService.GerarPreviewAsync(template.Tipo, template.Assunto, variaveis);
            var corpo = await _templateService.GerarPreviewAsync(template.Tipo, template.CorpoHtml, variaveis);

            // Enviar email de teste
            var emailEnviado = await _emailService.EnviarEmailAsync(
                request.EmailDestino,
                $"[TESTE] {assunto}",
                corpo,
                true
            );

            var usuarioId = ObterUsuarioId();
            _logger.LogInformation("📧 Teste de template - ID: {TemplateId}, Email: {Email}, Sucesso: {Sucesso}, Usuário: {UsuarioId}", 
                id, request.EmailDestino, emailEnviado, usuarioId);

            return Ok(new
            {
                enviado = emailEnviado,
                emailDestino = request.EmailDestino,
                assunto = assunto,
                dataEnvio = DateTime.UtcNow,
                template = new
                {
                    id = template.Id,
                    nome = template.Nome,
                    tipo = template.Tipo
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao testar template - ID: {Id}, Email: {Email}", id, request.EmailDestino);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Duplicar template existente
    /// </summary>
    /// <param name="id">ID do template a ser duplicado</param>
    /// <param name="requestBody">Corpo da requisição contendo o nome do novo template</param>
    /// <returns>Template duplicado</returns>
    [HttpPost("{id:int}/duplicar")]
    [ProducesResponseType(typeof(TemplatePersonalizadoResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TemplatePersonalizadoResponse>> DuplicarTemplate(
        int id, 
        [FromBody] DuplicarTemplateRequest requestBody)
    {
        try
        {
            // ✅ CORRIGIDO: Usar DTO tipado em vez de object
            if (string.IsNullOrWhiteSpace(requestBody.Nome))
            {
                return BadRequest(new { message = "Nome é obrigatório para duplicar template" });
            }

            // Verificar permissão
            if (!User.HasClaim("permission", "templates.criar"))
            {
                return Forbid("Usuário não tem permissão para criar templates");
            }

            var usuarioId = ObterUsuarioId();
            if (usuarioId == 0)
            {
                return Unauthorized();
            }

            // Obter template original
            var templates = await _templateService.ListarTemplatesAsync();
            var templateOriginal = templates.FirstOrDefault(t => t.Id == id);

            if (templateOriginal == null)
            {
                return NotFound(new { message = "Template não encontrado" });
            }

            // Criar novo template baseado no original
            var novoTemplateRequest = new CriarTemplateRequest
            {
                Nome = requestBody.Nome.Trim(),
                Tipo = templateOriginal.Tipo,
                Assunto = templateOriginal.Assunto,
                CorpoHtml = templateOriginal.CorpoHtml,
                CorpoTexto = templateOriginal.CorpoTexto,
                Descricao = $"Cópia de: {templateOriginal.Nome}"
            };

            var novoTemplate = await _templateService.CriarTemplateAsync(novoTemplateRequest, usuarioId);

            _logger.LogInformation("✅ Template duplicado - Original: {IdOriginal}, Novo: {IdNovo}, Nome: {Nome}, Usuário: {UsuarioId}", 
                id, novoTemplate.Id, requestBody.Nome, usuarioId);

            return CreatedAtAction(nameof(ObterTemplate), new { id = novoTemplate.Id }, novoTemplate);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("⚠️ Erro de validação ao duplicar template: {Erro}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao duplicar template - ID: {Id}", id);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Inicializar templates padrão do sistema
    /// </summary>
    /// <returns>Confirmação da inicialização</returns>
    [HttpPost("inicializar")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> InicializarTemplatesPadrao()
    {
        try
        {
            // Verificar permissão de admin
            if (!User.HasClaim("permission", "sistema.configurar"))
            {
                return Forbid("Usuário não tem permissão para inicializar templates do sistema");
            }

            await _templateService.InicializarTemplatesPadraoAsync();

            var usuarioId = ObterUsuarioId();
            _logger.LogInformation("✅ Templates padrão inicializados - Usuário: {UsuarioId}", usuarioId);

            return Ok(new
            {
                message = "Templates padrão inicializados com sucesso",
                dataInicializacao = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao inicializar templates padrão");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    // ✅ MÉTODO AUXILIAR PRIVADO
    private int ObterUsuarioId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}

/// <summary>
/// ✅ ADICIONADO: DTO para duplicar template
/// </summary>
public class DuplicarTemplateRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string Nome { get; set; } = string.Empty;
}