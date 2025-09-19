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

            // ✅ USAR HORÁRIO REAL DO SISTEMA
            usuario.UltimoLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(usuario);

            // Criar claims do usuário
            var identity = await CreateUserIdentityAsync(usuario);

            _logger.LogInformation("✅ Token gerado com sucesso para: {Email}", usuario.Email);

            // Retornar principal com claims
            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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

        // ✅ USAR HORÁRIO REAL DO SISTEMA
        var currentUtc = DateTime.UtcNow;
        var systemTimezone = TimeZoneInfo.Local.Id;
        
        _logger.LogInformation("🕐 Token criado em - UTC: {Utc}, Timezone: {Timezone}", 
            currentUtc, systemTimezone);

        // ✅ ADICIONAR AMBOS OS CLAIMS PARA COMPATIBILIDADE
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, usuario.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString())); // ✅ ADICIONAR

        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}"));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.PreferredUsername, usuario.Email!));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, usuario.Email!));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.EmailVerified, usuario.EmailConfirmed.ToString().ToLower()));

        // Claims personalizados
        identity.AddClaim(new Claim("nome", usuario.Nome));
        identity.AddClaim(new Claim("sobrenome", usuario.Sobrenome));
        identity.AddClaim(new Claim("ultimo_login", currentUtc.ToString("yyyy-MM-ddTHH:mm:ssZ")));
        identity.AddClaim(new Claim("timezone", systemTimezone));
        
        var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
        identity.AddClaim(new Claim("utc_offset", offset.ToString(@"hh\:mm\:ss")));

        // Obter papéis do usuário
        var papeis = await _userManager.GetRolesAsync(usuario);
        foreach (var papel in papeis)
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, papel));
        }

        // Obter permissões através dos papéis
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
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

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

            // ✅ ADICIONAR: Audience sempre no access token
            case OpenIddictConstants.Claims.Audience:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;

            // Claims personalizados sempre no access token
            case "nome":
            case "sobrenome":
            case "ultimo_login":
            case "permissao":
            case "timezone":
            case "utc_offset":
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp": 
                yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }
}