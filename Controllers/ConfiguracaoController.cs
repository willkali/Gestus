using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Gestus.Controllers;

/// <summary>
/// Controller para fornecer configurações dinâmicas para o cliente
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConfiguracaoController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfiguracaoController> _logger;

    public ConfiguracaoController(
        IConfiguration configuration,
        ILogger<ConfiguracaoController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Obtém as configurações necessárias para o cliente (front-end)
    /// </summary>
    /// <returns>Configurações do cliente</returns>
    [HttpGet("cliente")]
    [AllowAnonymous]
    public IActionResult ObterConfiguracaoCliente()
    {
        try
        {
            var request = HttpContext.Request;
            var scheme = request.Scheme; // http ou https
            var host = request.Host.Value;
            
            // Pegar a URL base dinamicamente ou usar configuração
            var baseUrl = _configuration.GetValue<string>("Client:BaseUrl") 
                         ?? $"{scheme}://{host}";

            var configuracao = new
            {
                BaseUrl = baseUrl,
                Nome = _configuration.GetValue<string>("App:Name", "Gestus"),
                Versao = _configuration.GetValue<string>("App:Version", "1.0.0"),
                Ambiente = _configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT", "Production"),
                // Outras configurações que o front pode precisar
                MaxTamanhoArquivo = _configuration.GetValue<long>("Upload:MaxFileSize", 10485760), // 10MB
                FormatosPermitidos = _configuration.GetSection("Upload:AllowedFormats").Get<string[]>() 
                                   ?? new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" }
            };

            return Ok(configuracao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter configuração do cliente");
            return StatusCode(500, new { error = "Erro interno do servidor" });
        }
    }
}