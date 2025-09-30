using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Gestus.Modelos;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Gestus.Services;

namespace Gestus.Controllers;

/// <summary>
/// Controlador responsável pela autenticação de usuários
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AutenticacaoController : ControllerBase
{
    private readonly UserManager<Usuario> _userManager;
    private readonly SignInManager<Usuario> _signInManager;
    private readonly RoleManager<Papel> _roleManager;
    private readonly ITimezoneService _timezoneService;
    private readonly ILogger<AutenticacaoController> _logger;

    public AutenticacaoController(
        UserManager<Usuario> userManager,
        SignInManager<Usuario> signInManager,
        RoleManager<Papel> roleManager,
        ITimezoneService timezoneService,
        ILogger<AutenticacaoController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _timezoneService = timezoneService;
        _logger = logger;
    }

    /// <summary>
    /// Realiza o login do usuário
    /// </summary>
    /// <param name="request">Dados de login</param>
    /// <returns>Token de acesso e informações do usuário</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(RespostaLogin), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] SolicitacaoLogin request)
    {
        try
        {
            _logger.LogInformation("🔐 Tentativa de login para: {Email}", request.Email);

            // Validar dados de entrada
            if (!ModelState.IsValid)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "DadosInvalidos",
                    Mensagem = "Dados de login inválidos",
                    Detalhes = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            // ✅ USAR TOKENS REAIS: Redirecionar para endpoint OpenIddict
            using var httpClient = new HttpClient();

            // Fazer requisição para nosso endpoint de token
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var tokenEndpoint = $"{baseUrl}/connect/token";

            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", "gestus_api"),
                new KeyValuePair<string, string>("client_secret", "gestus_api_secret_2024"),
                new KeyValuePair<string, string>("username", request.Email),
                new KeyValuePair<string, string>("password", request.Senha),
                new KeyValuePair<string, string>("scope", "openid profile email roles offline_access")
            });

            var response = await httpClient.PostAsync(tokenEndpoint, tokenRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("⚠️ Erro no token endpoint: {Error}", errorContent);

                return Unauthorized(new RespostaErro
                {
                    Erro = "CredenciaisInvalidas",
                    Mensagem = "Email ou senha incorretos"
                });
            }

            var tokenResponse = await response.Content.ReadAsStringAsync();
            var tokenData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(tokenResponse);

            var accessToken = tokenData.GetProperty("access_token").GetString()!;
            var expiresIn = tokenData.GetProperty("expires_in").GetInt32();
            var refreshToken = tokenData.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

            // ✅ BUSCAR USUÁRIO APÓS LOGIN (dados atualizados)
            var usuario = await _userManager.FindByEmailAsync(request.Email);
            var papeis = await _userManager.GetRolesAsync(usuario!);
            var permissoes = await ObterPermissoesUsuario(usuario!.Id);

            // ✅ CALCULAR EXPIRAÇÃO EM HORÁRIO LOCAL
            var agora = _timezoneService.GetCurrentLocal();
            var expiracao = agora.AddSeconds(expiresIn);

            _logger.LogInformation("✅ Login realizado - Local: {Local}, Expiracao: {Expiracao}",
                _timezoneService.FormatDateTimeWithTimezone(agora),
                _timezoneService.FormatDateTimeWithTimezone(expiracao));

            return Ok(new RespostaLogin
            {
                Sucesso = true,
                Token = accessToken,
                TipoToken = "Bearer",
                // ✅ USAR HORÁRIO LOCAL
                ExpiracaoEm = expiracao,
                RefreshToken = refreshToken ?? "",
                Usuario = new InformacoesUsuario
                {
                    Id = usuario!.Id,
                    Email = usuario.Email!,
                    Nome = usuario.Nome,
                    Sobrenome = usuario.Sobrenome,
                    NomeCompleto = usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}",
                    // ✅ CONVERTER UTC DO BANCO PARA HORÁRIO LOCAL
                    UltimoLogin = usuario.UltimoLogin.HasValue ?
                        _timezoneService.ToLocal(usuario.UltimoLogin.Value) :
                        null,
                    Papeis = papeis.ToList(),
                    Permissoes = permissoes,
                    // ✅ ADICIONAR INFORMAÇÕES DE TIMEZONE
                    InformacoesTimezone = _timezoneService.GetTimezoneDebugInfo()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro durante o login para: {Email}", request.Email);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno do servidor"
            });
        }
    }

    /// <summary>
    /// Renova o token de acesso usando o refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>Novo token de acesso</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RespostaLogin), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] SolicitacaoRefresh request)
    {
        try
        {
            _logger.LogInformation("🔄 Tentativa de renovação de token");

            if (!ModelState.IsValid || string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "RefreshTokenInvalido",
                    Mensagem = "Refresh token é obrigatório"
                });
            }

            // ✅ USAR O MESMO PADRÃO DO LOGIN: Redirecionar para o TokenController
            using var httpClient = new HttpClient();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var tokenEndpoint = $"{baseUrl}/connect/token";

            var tokenRequest = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", "gestus_api"),
            new KeyValuePair<string, string>("client_secret", "gestus_api_secret_2024"),
            new KeyValuePair<string, string>("refresh_token", request.RefreshToken)
        });

            var response = await httpClient.PostAsync(tokenEndpoint, tokenRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("⚠️ Erro no refresh token: {Error}", errorContent);

                return Unauthorized(new RespostaErro
                {
                    Erro = "RefreshTokenExpirado",
                    Mensagem = "Refresh token inválido ou expirado. Faça login novamente."
                });
            }

            var tokenResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("🔍 Resposta do refresh token: {Response}", tokenResponse);

            var tokenData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(tokenResponse);

            var accessToken = tokenData.GetProperty("access_token").GetString()!;
            var expiresIn = tokenData.GetProperty("expires_in").GetInt32();
            var newRefreshToken = tokenData.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : request.RefreshToken;

            // ✅ CORREÇÃO PRINCIPAL: OBTER USER ID DO REFRESH TOKEN VALIDADO NO OPENIDDICT
            // Em vez de tentar decodificar o JWT, vamos usar o endpoint de introspecção ou buscar pelos claims do contexto

            // Fazer uma requisição de introspecção do refresh token para obter as informações
            var introspectionEndpoint = $"{baseUrl}/connect/introspect";
            var introspectionRequest = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("token", request.RefreshToken),
            new KeyValuePair<string, string>("token_type_hint", "refresh_token"),
            new KeyValuePair<string, string>("client_id", "gestus_api"),
            new KeyValuePair<string, string>("client_secret", "gestus_api_secret_2024")
        });

            var introspectionResponse = await httpClient.PostAsync(introspectionEndpoint, introspectionRequest);

            if (!introspectionResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("⚠️ Erro na introspecção do refresh token");
                return Unauthorized(new RespostaErro
                {
                    Erro = "RefreshTokenInvalido",
                    Mensagem = "Não foi possível validar o refresh token"
                });
            }

            var introspectionResult = await introspectionResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("🔍 Resultado da introspecção: {Result}", introspectionResult);

            var introspectionData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(introspectionResult);

            // Verificar se o token está ativo
            if (!introspectionData.TryGetProperty("active", out var activeProperty) || !activeProperty.GetBoolean())
            {
                return Unauthorized(new RespostaErro
                {
                    Erro = "RefreshTokenExpirado",
                    Mensagem = "Refresh token inválido ou expirado"
                });
            }

            // Obter o subject (user ID) do refresh token
            if (!introspectionData.TryGetProperty("sub", out var subProperty))
            {
                return Unauthorized(new RespostaErro
                {
                    Erro = "RefreshTokenInvalido",
                    Mensagem = "Refresh token não contém ID de usuário"
                });
            }

            var userId = subProperty.GetString();
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userIdInt))
            {
                return Unauthorized(new RespostaErro
                {
                    Erro = "TokenInvalido",
                    Mensagem = "Token renovado não contém ID de usuário válido"
                });
            }

            // ✅ AGORA PODEMOS BUSCAR O USUÁRIO COM O ID CORRETO
            var usuario = await _userManager.FindByIdAsync(userId);
            if (usuario == null || !usuario.Ativo)
            {
                return Unauthorized(new RespostaErro
                {
                    Erro = "UsuarioInvalido",
                    Mensagem = "Usuário não encontrado ou inativo"
                });
            }

            var papeis = await _userManager.GetRolesAsync(usuario);
            var permissoes = await ObterPermissoesUsuario(usuario.Id);

            // ✅ CALCULAR NOVA EXPIRAÇÃO
            var agora = _timezoneService.GetCurrentLocal();
            var expiracao = agora.AddSeconds(expiresIn);

            _logger.LogInformation("✅ Token renovado com sucesso para: {Email}", usuario.Email);

            return Ok(new RespostaLogin
            {
                Sucesso = true,
                Token = accessToken,
                TipoToken = "Bearer",
                ExpiracaoEm = expiracao,
                RefreshToken = newRefreshToken ?? "",
                Usuario = new InformacoesUsuario
                {
                    Id = usuario.Id,
                    Email = usuario.Email!,
                    Nome = usuario.Nome,
                    Sobrenome = usuario.Sobrenome,
                    NomeCompleto = usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}",
                    UltimoLogin = usuario.UltimoLogin.HasValue ?
                        _timezoneService.ToLocal(usuario.UltimoLogin.Value) :
                        null,
                    Papeis = papeis.ToList(),
                    Permissoes = permissoes,
                    InformacoesTimezone = _timezoneService.GetTimezoneDebugInfo()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro durante renovação do token");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno do servidor"
            });
        }
    }

    /// <summary>
    /// Realiza o logout do usuário
    /// </summary>
    /// <returns>Confirmação de logout</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(RespostaSucesso), StatusCodes.Status200OK)]
    public IActionResult Logout() // ✅ Removido async - método síncrono
    {
        try
        {
            // Para logout real, invalidaríamos o token no OpenIddict
            // Por enquanto, apenas confirmamos o logout
            return Ok(new RespostaSucesso
            {
                Sucesso = true,
                Mensagem = "Logout realizado com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro durante logout");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno do servidor"
            });
        }
    }

    /// <summary>
    /// Obtém informações do usuário autenticado
    /// </summary>
    /// <returns>Informações do usuário logado</returns>
    [HttpGet("perfil")]
    [Authorize]
    [ProducesResponseType(typeof(InformacoesUsuario), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObterPerfil()
    {
        try
        {
            var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject) ??
                        User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userIdInt))
            {
                return Unauthorized(new RespostaErro
                {
                    Erro = "TokenInvalido",
                    Mensagem = "Token não contém ID de usuário válido"
                });
            }

            var usuario = await _userManager.FindByIdAsync(userId);
            if (usuario == null || !usuario.Ativo)
            {
                return Unauthorized(new RespostaErro
                {
                    Erro = "UsuarioInvalido",
                    Mensagem = "Usuário não encontrado ou inativo"
                });
            }

            var papeis = await _userManager.GetRolesAsync(usuario);
            var permissoes = await ObterPermissoesUsuario(usuario.Id);

            return Ok(new InformacoesUsuario
            {
                Id = usuario.Id,
                Email = usuario.Email!,
                Nome = usuario.Nome,
                Sobrenome = usuario.Sobrenome,
                NomeCompleto = usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}",
                // ✅ CONVERTER UTC PARA LOCAL
                UltimoLogin = usuario.UltimoLogin.HasValue ?
                    _timezoneService.ToLocal(usuario.UltimoLogin.Value) :
                    null,
                Papeis = papeis.ToList(),
                Permissoes = permissoes,
                // ✅ ADICIONAR INFORMAÇÕES DE TIMEZONE
                InformacoesTimezone = _timezoneService.GetTimezoneDebugInfo()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter perfil do usuário");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno do servidor"
            });
        }
    }

    /// <summary>
    /// Verifica se o token é válido
    /// </summary>
    /// <returns>Status do token</returns>
    [HttpGet("validar-token")]
    [Authorize] // ✅ USAR AUTHORIZE ao invés de validação manual
    [ProducesResponseType(typeof(RespostaValidacaoToken), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidarToken()
    {
        try
        {
            // ✅ CORRIGIR: OpenIddict usa claim "sub" para subject (user ID)
            var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject) ??
                        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                        User.FindFirstValue("sub");

            var email = User.FindFirstValue(OpenIddictConstants.Claims.Email) ??
                       User.FindFirstValue(ClaimTypes.Email);

            var name = User.FindFirstValue(OpenIddictConstants.Claims.Name) ??
                      User.FindFirstValue(ClaimTypes.Name);

            _logger.LogInformation("🔍 Claims do token - UserId: {UserId}, Email: {Email}, Name: {Name}",
                userId, email, name);

            // ✅ LOG TODOS OS CLAIMS para debug
            var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            _logger.LogInformation("🔍 Todos os claims: {@Claims}", allClaims);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("⚠️ Token válido mas sem claim de usuário");
                return Unauthorized(new RespostaErro
                {
                    Erro = "TokenInvalido",
                    Mensagem = "Token não contém informações válidas do usuário"
                });
            }

            // ✅ TENTAR CONVERTER PARA INT
            if (!int.TryParse(userId, out var userIdInt))
            {
                _logger.LogWarning("⚠️ UserId não é um número válido: {UserId}", userId);
                return Unauthorized(new RespostaErro
                {
                    Erro = "TokenInvalido",
                    Mensagem = "Formato de ID de usuário inválido"
                });
            }

            var usuario = await _userManager.FindByIdAsync(userId);
            if (usuario == null || !usuario.Ativo)
            {
                _logger.LogWarning("⚠️ Usuário não encontrado ou inativo: {UserId}", userId);
                return Unauthorized(new RespostaErro
                {
                    Erro = "UsuarioInvalido",
                    Mensagem = "Usuário não encontrado ou inativo"
                });
            }

            // Obter papéis e permissões
            var papeis = await _userManager.GetRolesAsync(usuario);
            var permissoes = await ObterPermissoesUsuario(usuario.Id);
            var expiracao = GetTokenExpiration();

            _logger.LogInformation("✅ Token validado com sucesso para usuário: {Email}", usuario.Email);

            return Ok(new RespostaValidacaoToken
            {
                Valido = true,
                UsuarioId = userId,
                Email = usuario.Email,
                Papeis = papeis.ToList(),
                Permissoes = permissoes,
                ExpiracaoEm = expiracao
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro durante validação de token");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno do servidor"
            });
        }
    }

    #region Métodos Privados

    /// <summary>
    /// Obtém todas as permissões de um usuário (através dos papéis)
    /// </summary>
    private async Task<List<string>> ObterPermissoesUsuario(int usuarioId)
    {
        var usuario = await _userManager.Users
            .Where(u => u.Id == usuarioId)
            .FirstOrDefaultAsync();

        if (usuario == null) return new List<string>();

        var papeis = await _userManager.GetRolesAsync(usuario);

        // Buscar permissões através dos papéis do usuário
        var permissoes = await _roleManager.Roles
            .Where(r => papeis.Contains(r.Name!))
            .SelectMany(r => r.PapelPermissoes
                .Where(pp => pp.Ativo)
                .Select(pp => pp.Permissao.Nome))
            .Distinct()
            .ToListAsync();

        return permissoes;
    }

    /// <summary>
    /// Obtém a expiração do token atual
    /// </summary>
    private DateTime? GetTokenExpiration()
    {
        // ✅ TENTAR DIFERENTES CLAIMS DE EXPIRAÇÃO
        var exp = User.FindFirstValue("exp") ??
                  User.FindFirstValue(OpenIddictConstants.Claims.ExpiresAt);

        if (long.TryParse(exp, out var expUnix))
        {
            return DateTimeOffset.FromUnixTimeSeconds(expUnix).DateTime;
        }
        return null;
    }

    #endregion
}

#region DTOs de Autenticação

/// <summary>
/// Dados para solicitação de login
/// </summary>
public class SolicitacaoLogin
{
    /// <summary>
    /// Email do usuário
    /// </summary>
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email deve ter formato válido")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Senha do usuário
    /// </summary>
    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(6, ErrorMessage = "Senha deve ter pelo menos 6 caracteres")]
    public string Senha { get; set; } = string.Empty;

    /// <summary>
    /// Lembrar login (manter sessão)
    /// </summary>
    public bool LembrarLogin { get; set; } = false;
}

/// <summary>
/// Dados para renovação de token
/// </summary>
public class SolicitacaoRefresh
{
    /// <summary>
    /// Refresh token
    /// </summary>
    [Required(ErrorMessage = "Refresh token é obrigatório")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Resposta de login bem-sucedido
/// </summary>
public class RespostaLogin
{
    /// <summary>
    /// Indica se o login foi bem-sucedido
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Token de acesso
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do token (Bearer)
    /// </summary>
    public string TipoToken { get; set; } = "Bearer";

    /// <summary>
    /// Data/hora de expiração do token
    /// </summary>
    public DateTime ExpiracaoEm { get; set; }

    /// <summary>
    /// Token para renovação
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Informações do usuário logado
    /// </summary>
    public InformacoesUsuario Usuario { get; set; } = null!;
}

/// <summary>
/// Resposta de renovação de token
/// </summary>
public class RespostaToken
{
    /// <summary>
    /// Novo token de acesso
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do token
    /// </summary>
    public string TipoToken { get; set; } = "Bearer";

    /// <summary>
    /// Data/hora de expiração
    /// </summary>
    public DateTime ExpiracaoEm { get; set; }

    /// <summary>
    /// Novo refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Informações do usuário
/// </summary>
public class InformacoesUsuario
{
    /// <summary>
    /// ID do usuário
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Email do usuário
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Nome do usuário
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Sobrenome do usuário
    /// </summary>
    public string Sobrenome { get; set; } = string.Empty;

    /// <summary>
    /// Nome completo do usuário
    /// </summary>
    public string NomeCompleto { get; set; } = string.Empty;

    /// <summary>
    /// Data/hora do último login
    /// </summary>
    public DateTime? UltimoLogin { get; set; }

    /// <summary>
    /// Lista de papéis do usuário
    /// </summary>
    public List<string> Papeis { get; set; } = new();

    /// <summary>
    /// Lista de permissões do usuário
    /// </summary>
    public List<string> Permissoes { get; set; } = new();
    public object? InformacoesTimezone { get; set; }
}

/// <summary>
/// Resposta de validação de token
/// </summary>
public class RespostaValidacaoToken
{
    /// <summary>
    /// Indica se o token é válido
    /// </summary>
    public bool Valido { get; set; }

    /// <summary>
    /// ID do usuário
    /// </summary>
    public string? UsuarioId { get; set; }

    /// <summary>
    /// Email do usuário
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Papéis do usuário
    /// </summary>
    public List<string> Papeis { get; set; } = new();

    /// <summary>
    /// Lista de permissões do usuário
    /// </summary>
    public List<string> Permissoes { get; set; } = new();

    /// <summary>
    /// Data/hora de expiração do token
    /// </summary>
    public DateTime? ExpiracaoEm { get; set; }
    public object? TimezoneInfo { get; set; }
}

/// <summary>
/// Resposta genérica de sucesso
/// </summary>
public class RespostaSucesso
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Mensagem de sucesso
    /// </summary>
    public string Mensagem { get; set; } = string.Empty;
}

/// <summary>
/// Resposta genérica de erro
/// </summary>
public class RespostaErro
{
    /// <summary>
    /// Código do erro
    /// </summary>
    public string Erro { get; set; } = string.Empty;

    /// <summary>
    /// Mensagem do erro
    /// </summary>
    public string Mensagem { get; set; } = string.Empty;

    /// <summary>
    /// Detalhes adicionais do erro
    /// </summary>
    public List<string>? Detalhes { get; set; }
}

/// <summary>
/// Informações do token gerado
/// </summary>
internal class TokenInfo
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiracaoEm { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}

#endregion