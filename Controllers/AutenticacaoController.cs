using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Gestus.Modelos;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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
    private readonly ILogger<AutenticacaoController> _logger;

    public AutenticacaoController(
        UserManager<Usuario> userManager,
        SignInManager<Usuario> signInManager,
        RoleManager<Papel> roleManager,
        ILogger<AutenticacaoController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
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

            // 1. Validar dados de entrada
            if (!ModelState.IsValid)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "DadosInvalidos",
                    Mensagem = "Dados de login inválidos",
                    Detalhes = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            // 2. Buscar usuário por email
            var usuario = await _userManager.FindByEmailAsync(request.Email);
            if (usuario == null)
            {
                _logger.LogWarning("⚠️ Tentativa de login com email inexistente: {Email}", request.Email);
                return Unauthorized(new RespostaErro
                {
                    Erro = "CredenciaisInvalidas",
                    Mensagem = "Email ou senha incorretos"
                });
            }

            // 3. Verificar se usuário está ativo
            if (!usuario.Ativo)
            {
                _logger.LogWarning("⚠️ Tentativa de login com usuário inativo: {Email}", request.Email);
                return Unauthorized(new RespostaErro
                {
                    Erro = "UsuarioInativo",
                    Mensagem = "Usuário está inativo. Entre em contato com o administrador."
                });
            }

            // 4. Verificar senha
            var resultadoLogin = await _signInManager.CheckPasswordSignInAsync(usuario, request.Senha, true);
            
            if (resultadoLogin.IsLockedOut)
            {
                _logger.LogWarning("⚠️ Usuário bloqueado: {Email}", request.Email);
                return Unauthorized(new RespostaErro
                {
                    Erro = "UsuarioBloqueado",
                    Mensagem = "Usuário temporariamente bloqueado devido a muitas tentativas de login incorretas"
                });
            }

            if (!resultadoLogin.Succeeded)
            {
                _logger.LogWarning("⚠️ Senha incorreta para: {Email}", request.Email);
                return Unauthorized(new RespostaErro
                {
                    Erro = "CredenciaisInvalidas",
                    Mensagem = "Email ou senha incorretos"
                });
            }

            // 5. Atualizar último login
            usuario.UltimoLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(usuario);

            // 6. Obter papéis e permissões do usuário
            var papeis = await _userManager.GetRolesAsync(usuario);
            var permissoes = await ObterPermissoesUsuario(usuario.Id);

            // 7. Gerar token
            var token = await GerarTokenAcesso(usuario, papeis, permissoes);

            _logger.LogInformation("✅ Login realizado com sucesso para: {Email}", request.Email);

            return Ok(new RespostaLogin
            {
                Sucesso = true,
                Token = token.Token,
                TipoToken = "Bearer",
                ExpiracaoEm = token.ExpiracaoEm,
                RefreshToken = token.RefreshToken,
                Usuario = new InformacoesUsuario
                {
                    Id = usuario.Id,
                    Email = usuario.Email!,
                    Nome = usuario.Nome,
                    Sobrenome = usuario.Sobrenome,
                    NomeCompleto = usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}",
                    UltimoLogin = usuario.UltimoLogin,
                    Papeis = papeis.ToList(),
                    Permissoes = permissoes
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
    [ProducesResponseType(typeof(RespostaToken), StatusCodes.Status200OK)]
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

            // TODO: Implementar validação do refresh token com OpenIddict
            // Por enquanto, retornamos erro para implementar depois
            
            return Unauthorized(new RespostaErro
            {
                Erro = "RefreshTokenExpirado",
                Mensagem = "Refresh token inválido ou expirado. Faça login novamente."
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
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("🚪 Logout do usuário: {UserId}", userId);

            await _signInManager.SignOutAsync();

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userIdInt))
            {
                return Unauthorized();
            }

            var usuario = await _userManager.FindByIdAsync(userId);
            if (usuario == null || !usuario.Ativo)
            {
                return Unauthorized();
            }

            var papeis = await _userManager.GetRolesAsync(usuario);
            var permissoes = await ObterPermissoesUsuario(userIdInt);

            return Ok(new InformacoesUsuario
            {
                Id = usuario.Id,
                Email = usuario.Email!,
                Nome = usuario.Nome,
                Sobrenome = usuario.Sobrenome,
                NomeCompleto = usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}",
                UltimoLogin = usuario.UltimoLogin,
                Papeis = papeis.ToList(),
                Permissoes = permissoes
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
    [ProducesResponseType(typeof(RespostaValidacaoToken), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidarToken()
    {
        try
        {
            // ✅ VALIDAÇÃO TEMPORÁRIA PARA MOCK TOKENS
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new RespostaErro
                {
                    Erro = "TokenAusente",
                    Mensagem = "Token de autorização não fornecido"
                });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            
            // Validar se é um mock token válido (formato: mock_token_{userId}_{timestamp})
            if (!token.StartsWith("mock_token_"))
            {
                return Unauthorized(new RespostaErro
                {
                    Erro = "TokenInvalido",
                    Mensagem = "Formato de token inválido"
                });
            }

            // ✅ CORRIGIDO: Extrair userId do mock token corretamente
            var tokenParts = token.Split('_');
            
            // Debug log para ver as partes do token
            _logger.LogInformation("🔍 Token parts: {Parts}", string.Join(" | ", tokenParts));
            
            // Formato esperado: ["mock", "token", "userId", "timestamp"]
            if (tokenParts.Length != 4)
            {
                return Unauthorized(new RespostaErro
                {
                    Erro = "TokenInvalido",
                    Mensagem = $"Token corrompido - partes: {tokenParts.Length}"
                });
            }

            // ✅ CORRIGIDO: userId está na posição 2 (não 3)
            if (!int.TryParse(tokenParts[2], out var userId))
            {
                return Unauthorized(new RespostaErro
                {
                    Erro = "TokenInvalido",
                    Mensagem = $"UserId inválido no token: {tokenParts[2]}"
                });
            }

            // Buscar usuário pelo ID extraído do token
            var usuario = await _userManager.FindByIdAsync(userId.ToString());
            if (usuario == null || !usuario.Ativo)
            {
                return Unauthorized(new RespostaErro
                {
                    Erro = "UsuarioInvalido",
                    Mensagem = "Usuário não encontrado ou inativo"
                });
            }

            // Obter papéis e permissões
            var papeis = await _userManager.GetRolesAsync(usuario);
            var permissoes = await ObterPermissoesUsuario(usuario.Id);

            _logger.LogInformation("✅ Token validado para usuário: {Email} (ID: {UserId})", usuario.Email, usuario.Id);

            return Ok(new RespostaValidacaoToken
            {
                Valido = true,
                UsuarioId = usuario.Id.ToString(),
                Email = usuario.Email,
                Papeis = papeis.ToList(),
                Permissoes = permissoes,
                ExpiracaoEm = DateTime.UtcNow.AddHours(1) // Mock expiration
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao validar token");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno do servidor"
            });
        }
    }

    #region Métodos Privados

    /// <summary>
    /// Gera token de acesso para o usuário
    /// </summary>
    private async Task<TokenInfo> GerarTokenAcesso(Usuario usuario, IList<string> papeis, List<string> permissoes)
    {
        // TODO: Implementar geração de token JWT com OpenIddict
        // Por enquanto, retornar token mock para desenvolvimento
        
        var expiracaoEm = DateTime.UtcNow.AddHours(1);
        
        return new TokenInfo
        {
            Token = $"mock_token_{usuario.Id}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            ExpiracaoEm = expiracaoEm,
            RefreshToken = $"mock_refresh_{usuario.Id}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
        };
    }

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
        var exp = User.FindFirstValue("exp");
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