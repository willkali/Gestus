using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gestus.Services;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gestus.Controllers;

/// <summary>
/// Controller para informações do sistema
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SistemaController : ControllerBase
{
    private readonly ILogger<SistemaController> _logger;
    private readonly ITimezoneService _timezoneService;
    private readonly HealthCheckService _healthCheckService;

    public SistemaController(
        ILogger<SistemaController> logger, 
        ITimezoneService timezoneService,
        HealthCheckService healthCheckService)
    {
        _logger = logger;
        _timezoneService = timezoneService;
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Obtém informações básicas do sistema (público)
    /// </summary>
    /// <returns>Informações básicas do sistema</returns>
    [HttpGet("info")]
    [AllowAnonymous]
    public ActionResult<object> ObterInformacoesBasicas()
    {
        try
        {
            var systemInfo = LoadSystemInfo();
            
            var informacoes = new
            {
                name = systemInfo.Basic.Name,
                fullName = systemInfo.Basic.FullName,
                version = systemInfo.Basic.Version,
                description = systemInfo.Basic.Description,
                timestamp = _timezoneService.ToIso8601String(_timezoneService.GetCurrentLocal()),
                timezone = new
                {
                    id = _timezoneService.GetSystemTimezone(),
                    display = _timezoneService.GetTimezoneDisplay(),
                    offset = _timezoneService.GetUtcOffset().ToString(@"hh\:mm")
                }
            };

            _logger.LogDebug("📋 Informações básicas do sistema solicitadas");
            
            return Ok(informacoes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter informações básicas do sistema");
            return StatusCode(500, new { message = "Erro ao carregar informações do sistema" });
        }
    }

    /// <summary>
    /// Obtém informações detalhadas do sistema (requer autenticação)
    /// </summary>
    /// <returns>Informações detalhadas do sistema</returns>
    [HttpGet("info/detalhada")]
    [Authorize]
    public ActionResult<object> ObterInformacoesDetalhadas()
    {
        try
        {
            var systemInfo = LoadSystemInfo();

            var informacoesDetalhadas = new
            {
                // ✅ TUDO DO JSON
                basic = systemInfo.Basic,
                backend = systemInfo.Backend,
                frontend = systemInfo.Frontend,
                timezone = _timezoneService.GetTimezoneDebugInfo(),
                company = systemInfo.Company
            };

            _logger.LogInformation("📋 Informações detalhadas do sistema solicitadas por usuário autenticado");
            
            return Ok(informacoesDetalhadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter informações detalhadas do sistema");
            return StatusCode(500, new { message = "Erro ao carregar informações do sistema" });
        }
    }

    /// <summary>
    /// Health check usando o serviço existente
    /// </summary>
    /// <returns>Status de saúde</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> HealthCheck()
    {
        try
        {
            var systemInfo = LoadSystemInfo();
            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            var health = new
            {
                status = healthReport.Status == HealthStatus.Healthy 
                    ? systemInfo.Messages.HealthyStatus 
                    : healthReport.Status.ToString().ToLower(),
                timestamp = _timezoneService.ToIso8601String(_timezoneService.GetCurrentLocal()),
                checks = healthReport.Entries.ToDictionary(
                    x => x.Key,
                    x => new
                    {
                        status = x.Value.Status.ToString().ToLower(),
                        description = x.Value.Description,
                        duration = x.Value.Duration.TotalMilliseconds + "ms"
                    }
                )
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro no health check");
            return StatusCode(500, new 
            { 
                status = "unhealthy",
                timestamp = _timezoneService.ToIso8601String(_timezoneService.GetCurrentLocal()),
                message = "Erro ao verificar saúde do sistema"
            });
        }
    }

    #region Métodos Auxiliares

    /// <summary>
    /// Carrega informações do sistema do arquivo JSON
    /// </summary>
    private SystemInfoModel LoadSystemInfo()
    {
        var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "system-info.json");
        
        if (!System.IO.File.Exists(jsonPath))
        {
            _logger.LogError("❌ Arquivo system-info.json não encontrado em: {JsonPath}", jsonPath);
            throw new FileNotFoundException($"Arquivo de configuração do sistema não encontrado: {jsonPath}");
        }

        try
        {
            var jsonContent = System.IO.File.ReadAllText(jsonPath);
            var systemInfo = JsonSerializer.Deserialize<SystemInfoModel>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (systemInfo == null)
            {
                _logger.LogError("❌ Erro ao deserializar system-info.json - conteúdo inválido");
                throw new InvalidOperationException("Arquivo system-info.json contém dados inválidos");
            }

            return systemInfo;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ Erro ao fazer parse do system-info.json");
            throw new InvalidOperationException("Arquivo system-info.json contém JSON inválido", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao carregar system-info.json");
            throw;
        }
    }

    #endregion

    #region Models

    public class SystemInfoModel
    {
        public BasicInfo Basic { get; set; } = new();
        public CompanyInfo Company { get; set; } = new();
        public BackendInfo Backend { get; set; } = new();
        public FrontendInfo Frontend { get; set; } = new();
        public MessagesInfo Messages { get; set; } = new();
    }

    public class BasicInfo
    {
        public string Name { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Version { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class CompanyInfo
    {
        public string Name { get; set; } = "";
        public string Website { get; set; } = "";
        public string Support { get; set; } = "";
        public string Copyright { get; set; } = "";
    }

    public class BackendInfo
    {
        public string Framework { get; set; } = "";
        public string Database { get; set; } = "";
        public string Orm { get; set; } = "";
        public string Authentication { get; set; } = "";
        public string Logging { get; set; } = "";
        public string Monitoring { get; set; } = "";
        public string HealthChecks { get; set; } = "";
        public string Validation { get; set; } = "";
        public string Mapping { get; set; } = "";
        public string Resilience { get; set; } = "";
    }

    public class FrontendInfo
    {
        public string Framework { get; set; } = "";
        public string Ui { get; set; } = "";
        public string Routing { get; set; } = "";
        public string State { get; set; } = "";
        public string Http { get; set; } = "";
        public string Forms { get; set; } = "";
        public string Icons { get; set; } = "";
        public string Styling { get; set; } = "";
    }

    public class MessagesInfo
    {
        public string HealthyStatus { get; set; } = "";
        public string ActiveStatus { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }

    #endregion
}