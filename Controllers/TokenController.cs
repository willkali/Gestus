using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Gestus.Modelos;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using Microsoft.AspNetCore;
using Gestus.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Gestus.Controllers;

/// <summary>
/// Controlador de tokens OpenIddict
/// </summary>
[ApiController]
[Route("connect")]
public class TokenController : ControllerBase
{
    private readonly UserManager<Usuario> _userManager;
    private readonly SignInManager<Usuario> _signInManager;
    private readonly RoleManager<Papel> _roleManager;
    private readonly ILogger<TokenController> _logger;
    private readonly ITimezoneService _timezoneService;

    public TokenController(
        UserManager<Usuario> userManager,
        SignInManager<Usuario> signInManager,
        RoleManager<Papel> roleManager,
        ILogger<TokenController> logger,
        ITimezoneService timezoneService) // ✅ ADICIONAR
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _logger = logger;
        _timezoneService = timezoneService; // ✅ ADICIONAR
    }

    /// <summary>
    /// Endpoint de token do OpenIddict
    /// </summary>
    [HttpPost("token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                     throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordGrant(request);
        }

        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshTokenGrant(request);
        }

        if (request.IsClientCredentialsGrantType())
        {
            return await HandleClientCredentialsGrant(request);
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    /// <summary>
    /// Manipula grant type password (Resource Owner Password Credentials)
    /// </summary>
    private async Task<IActionResult> HandlePasswordGrant(OpenIddictRequest request)
    {
        try
        {
            _logger.LogInformation("🔐 Processando grant type password para: {Username}", request.Username);

            // ✅ LOG DOS SCOPES SOLICITADOS
            _logger.LogInformation("🔍 Scopes solicitados: {Scopes}", request.Scope ?? "NENHUM");

            // Buscar usuário por email/username
            var usuario = await _userManager.FindByEmailAsync(request.Username!) ??
                          await _userManager.FindByNameAsync(request.Username!);

            if (usuario == null)
            {
                _logger.LogWarning("⚠️ Usuário não encontrado: {Username}", request.Username);

                var properties = new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Email ou senha incorretos."
                });

                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Verificar se usuário está ativo
            if (!usuario.Ativo)
            {
                _logger.LogWarning("⚠️ Usuário inativo: {Email}", usuario.Email);

                var properties = new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Usuário está inativo."
                });

                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Verificar senha
            var resultado = await _signInManager.CheckPasswordSignInAsync(usuario, request.Password!, lockoutOnFailure: true);

            if (resultado.IsLockedOut)
            {
                _logger.LogWarning("⚠️ Usuário bloqueado: {Email}", usuario.Email);

                var properties = new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Usuário temporariamente bloqueado."
                });

                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            if (!resultado.Succeeded)
            {
                _logger.LogWarning("⚠️ Senha incorreta para: {Email}", usuario.Email);

                var properties = new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Email ou senha incorretos."
                });

                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // USAR HORÁRIO REAL DO SISTEMA
            usuario.UltimoLogin = _timezoneService.GetCurrentUtc();
            await _userManager.UpdateAsync(usuario);

            // ✅ CRIAR IDENTIDADE COM SCOPES EXPLÍCITOS
            var identity = await CreateUserIdentityAsync(usuario);

            // ✅ CONFIGURAR SCOPES EXPLICITAMENTE PARA GERAR REFRESH TOKEN
            var scopes = request.Scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

            // ✅ GARANTIR QUE offline_access ESTEJA PRESENTE
            var scopesList = scopes.ToList();
            if (!scopesList.Contains(OpenIddictConstants.Scopes.OfflineAccess))
            {
                scopesList.Add(OpenIddictConstants.Scopes.OfflineAccess);
                _logger.LogInformation("🔧 Adicionando scope offline_access automaticamente");
            }

            // ✅ CONFIGURAR SCOPES NO PRINCIPAL
            identity.SetScopes(scopesList.ToImmutableArray());

            // ✅ LOG DOS SCOPES CONFIGURADOS
            _logger.LogInformation("🔍 Scopes configurados no identity: {ConfiguredScopes}", string.Join(", ", scopesList));

            _logger.LogInformation("✅ Token gerado com sucesso para: {Email} em {LocalTime}",
                usuario.Email,
                _timezoneService.FormatDateTimeWithTimezone(_timezoneService.GetCurrentLocal()));

            // ✅ RETORNAR PRINCIPAL COM SCOPES CONFIGURADOS
            var principal = new ClaimsPrincipal(identity);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro durante geração de token");

            var properties = new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.ServerError,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Erro interno do servidor."
            });

            return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
    }

    /// <summary>
    /// Manipula refresh token grant
    /// </summary>
    private async Task<IActionResult> HandleRefreshTokenGrant(OpenIddictRequest request)
    {
        // Obter informações do refresh token
        var info = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var userId = info?.Principal?.GetClaim(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userIdInt))
        {
            var properties = new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Refresh token inválido."
            });

            return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var usuario = await _userManager.FindByIdAsync(userId);
        if (usuario == null || !usuario.Ativo)
        {
            var properties = new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Usuário não encontrado ou inativo."
            });

            return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // Criar nova identidade
        var identity = await CreateUserIdentityAsync(usuario);

        _logger.LogInformation("🔄 Refresh token processado para: {Email}", usuario.Email);

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Manipula client credentials grant
    /// </summary>
    private Task<IActionResult> HandleClientCredentialsGrant(OpenIddictRequest request)
    {
        // Para aplicações machine-to-machine
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, request.ClientId!));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, "Sistema"));

        return Task.FromResult<IActionResult>(SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme));
    }

    /// <summary>
    /// Cria identidade do usuário com claims e permissões
    /// </summary>
    private async Task<ClaimsIdentity> CreateUserIdentityAsync(Usuario usuario)
    {
        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        // ✅ USAR TimezoneService para obter informações consistentes
        var currentUtc = _timezoneService.GetCurrentUtc();
        var currentLocal = _timezoneService.GetCurrentLocal();
        var systemTimezone = _timezoneService.GetSystemTimezone();
        var utcOffset = _timezoneService.GetUtcOffset();

        _logger.LogInformation("🕐 Token criado - UTC: {Utc}, Local: {Local}, Timezone: {Timezone}, Offset: {Offset}",
            _timezoneService.FormatDateTime(currentUtc),
            _timezoneService.FormatDateTime(currentLocal),
            systemTimezone,
            utcOffset);

        // Claims básicos
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, usuario.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}"));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.PreferredUsername, usuario.Email!));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, usuario.Email!));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.EmailVerified, usuario.EmailConfirmed.ToString().ToLower()));
        identity.AddClaim(new Claim("nome", usuario.Nome));
        identity.AddClaim(new Claim("sobrenome", usuario.Sobrenome));
        identity.AddClaim(new Claim("ultimo_login", _timezoneService.ToIso8601String(currentUtc)));
        identity.AddClaim(new Claim("ultimo_login_local", _timezoneService.FormatDateTimeWithTimezone(currentLocal)));
        identity.AddClaim(new Claim("timezone", systemTimezone));
        identity.AddClaim(new Claim("timezone_display", _timezoneService.GetTimezoneDisplay()));
        identity.AddClaim(new Claim("utc_offset", utcOffset.ToString(@"hh\:mm")));
        identity.AddClaim(new Claim("utc_offset_seconds", utcOffset.TotalSeconds.ToString()));

        // Obter papéis do usuário
        var papeis = await _userManager.GetRolesAsync(usuario);
        foreach (var papel in papeis)
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, papel));
        }

        // Se for SuperAdmin, conceder coringa de permissão total
        if (papeis.Contains("SuperAdmin"))
        {
            identity.AddClaim(new Claim("permissao", "*"));
        }

        // Obter permissões através dos papéis (mantido para perfis não SuperAdmin)
        var permissoes = await ObterPermissoesUsuario(usuario.Id);
        foreach (var permissao in permissoes)
        {
            identity.AddClaim(new Claim("permissao", permissao));
        }

        // Configurar audiência
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Audience, "gestus_api"));

        // ✅ ATUALIZAR ÚLTIMO LOGIN
        usuario.UltimoLogin = currentUtc;
        await _userManager.UpdateAsync(usuario);

        // Configurar destinos dos claims
        foreach (var claim in identity.Claims)
        {
            claim.SetDestinations(GetDestinations(claim));
        }

        return identity;
    }

    /// <summary>
    /// Obtém permissões do usuário através dos papéis
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
    /// Define destinos dos claims (access token vs identity token)
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (claim.Subject!.HasScope(OpenIddictConstants.Scopes.Profile))
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case OpenIddictConstants.Claims.Email:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (claim.Subject!.HasScope(OpenIddictConstants.Scopes.Email))
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (claim.Subject!.HasScope(OpenIddictConstants.Scopes.Roles))
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case OpenIddictConstants.Claims.Audience:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;

            case "nome":
            case "sobrenome":
            case "ultimo_login":
            case "ultimo_login_local":
            case "permissao":
            case "timezone":
            case "timezone_display":
            case "utc_offset":
            case "utc_offset_seconds":
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;

            case "AspNet.Identity.SecurityStamp":
                yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }
}