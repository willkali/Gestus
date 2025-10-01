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
using Gestus.Dados;

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
    private readonly GestusDbContexto _context;
    private readonly UsuarioLoginService _loginService;

    public AutenticacaoController(
        UserManager<Usuario> userManager,
        SignInManager<Usuario> signInManager,
        RoleManager<Papel> roleManager,
        ITimezoneService timezoneService,
        GestusDbContexto context,
        UsuarioLoginService loginService,
        ILogger<AutenticacaoController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _timezoneService = timezoneService;
        _context = context;
        _loginService = loginService;
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

            // ✅ PRIMEIRO: Verificar se o usuário existe ANTES da validação via OpenIddict
            var usuarioExistente = await _userManager.FindByEmailAsync(request.Email);

            if (usuarioExistente == null)
            {
                _logger.LogWarning("❌ Tentativa de login com email inexistente: {Email}", request.Email);

                return Unauthorized(new RespostaErro
                {
                    Erro = "EmailIncorreto",
                    Mensagem = "Email não encontrado no sistema",
                    Detalhes = new List<string>
                {
                    "Verifique se o email está digitado corretamente",
                    "Certifique-se de usar o email cadastrado no sistema",
                    "Entre em contato com o administrador se necessário"
                }
                });
            }

            // ✅ SEGUNDO: Fazer validação via OpenIddict para verificar senha
            using var httpClient = new HttpClient();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var tokenEndpoint = $"{baseUrl}/connect/token";

            var tokenRequest = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", request.Email),
            new KeyValuePair<string, string>("password", request.Senha),
            new KeyValuePair<string, string>("client_id", "gestus_api"),
            new KeyValuePair<string, string>("client_secret", "gestus_api_secret_2024"),
            new KeyValuePair<string, string>("scope", "openid profile email roles offline_access")
        });

            var response = await httpClient.PostAsync(tokenEndpoint, tokenRequest);

            // ✅ DEPOIS da validação: Registrar tentativa (sempre)
            await _loginService.RegistrarTentativaLoginAsync(request.Email);

            // ✅ TERCEIRO: Tratar falha na validação (senha incorreta)
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("❌ Falha na autenticação para {Email}: {StatusCode} - {Error}",
                    request.Email, response.StatusCode, errorContent);

                // ✅ RENOMEAR: usar 'usuarioParaContador' em vez de 'usuarioAtualizado'
                var usuarioParaContador = await _userManager.FindByEmailAsync(request.Email);
                
                if (usuarioParaContador != null)
                {
                    // ✅ Calcular tentativas restantes
                    var maxTentativas = _userManager.Options.Lockout.MaxFailedAccessAttempts;
                    var tentativasAtuais = usuarioParaContador.AccessFailedCount;
                    var tentativasRestantes = maxTentativas - tentativasAtuais;
                    
                    _logger.LogInformation("📊 Tentativas de login para {Email}: {TentativasAtuais}/{MaxTentativas} - Restantes: {Restantes}", 
                        request.Email, tentativasAtuais, maxTentativas, tentativasRestantes);

                    var detalhes = new List<string>
                    {
                        "A senha informada está incorreta"
                    };

                    // ✅ Informar tentativas restantes apenas se o usuário não estiver bloqueado
                    if (!usuarioParaContador.LockoutEnabled || !usuarioParaContador.LockoutEnd.HasValue || usuarioParaContador.LockoutEnd <= DateTimeOffset.UtcNow)
                    {
                        if (tentativasRestantes > 1)
                        {
                            detalhes.Add($"Você tem {tentativasRestantes} tentativas restantes antes do bloqueio temporário");
                        }
                        else if (tentativasRestantes == 1)
                        {
                            detalhes.Add("⚠️ ATENÇÃO: Você tem apenas 1 tentativa restante antes do bloqueio temporário");
                            detalhes.Add("Certifique-se da senha antes de tentar novamente");
                        }
                        else
                        {
                            detalhes.Add("Conta temporariamente bloqueada devido a muitas tentativas incorretas");
                            if (usuarioParaContador.LockoutEnd.HasValue)
                            {
                                var tempoRestante = usuarioParaContador.LockoutEnd.Value - DateTimeOffset.UtcNow;
                                if (tempoRestante.TotalMinutes > 0)
                                {
                                    detalhes.Add($"Tente novamente em {Math.Ceiling(tempoRestante.TotalMinutes)} minutos");
                                }
                            }
                        }
                    }
                    else
                    {
                        // ✅ Usuário está bloqueado
                        detalhes.Clear();
                        detalhes.Add("Conta temporariamente bloqueada devido a muitas tentativas incorretas");
                        
                        if (usuarioParaContador.LockoutEnd.HasValue)
                        {
                            var tempoRestante = usuarioParaContador.LockoutEnd.Value - DateTimeOffset.UtcNow;
                            if (tempoRestante.TotalMinutes > 0)
                            {
                                detalhes.Add($"Tente novamente em {Math.Ceiling(tempoRestante.TotalMinutes)} minutos");
                            }
                            else
                            {
                                detalhes.Add("O bloqueio expirou, você pode tentar fazer login novamente");
                            }
                        }
                    }

                    return Unauthorized(new RespostaErro
                    {
                        Erro = tentativasRestantes <= 0 ? "ContaBloqueada" : "SenhaIncorreta",
                        Mensagem = tentativasRestantes <= 0 ? "Conta temporariamente bloqueada" : "Senha incorreta",
                        Detalhes = detalhes
                    });
                }

                // ✅ Fallback se não conseguir buscar o usuário
                return Unauthorized(new RespostaErro
                {
                    Erro = "SenhaIncorreta",
                    Mensagem = "Senha incorreta",
                    Detalhes = new List<string> { "A senha informada está incorreta" }
                });
            }

            // ✅ QUARTO: Processar login bem-sucedido
            var tokenContent = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);

            // Buscar usuário completo
            var usuario = await _userManager.Users
    .Include(u => u.UsuarioPapeis)
        .ThenInclude(up => up.Papel)
    .AsNoTracking()
    .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.ToUpper());

            if (usuario == null)
            {
                return Unauthorized(new RespostaErro
                {
                    Erro = "UsuarioNaoEncontrado",
                    Mensagem = "Usuário não encontrado"
                });
            }

            // ✅ Verificar se usuário está ativo
            if (!usuario.Ativo)
            {
                _logger.LogWarning("⚠️ Tentativa de login em conta inativa: {Email}", request.Email);
                return Unauthorized(new RespostaErro
                {
                    Erro = "ContaInativa",
                    Mensagem = "Esta conta está inativa",
                    Detalhes = new List<string>
        {
            "Sua conta foi desativada pelo administrador",
            "Entre em contato com o suporte para reativação"
        }
                });
            }

            // ✅ Registrar login bem-sucedido
            await _loginService.RegistrarLoginSucessoAsync(request.Email);

            // ✅ Buscar usuário novamente para ter os dados atualizados após o registro de login
            var usuarioAtualizado = await _userManager.Users
                .Include(u => u.UsuarioPapeis)
                    .ThenInclude(up => up.Papel)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.ToUpper());

            // ✅ VERIFICAÇÃO ADICIONAL DE SEGURANÇA (resolver CS8602)
            if (usuarioAtualizado == null)
            {
                _logger.LogError("❌ Usuário não encontrado após atualização de login: {Email}", request.Email);
                return StatusCode(500, new RespostaErro
                {
                    Erro = "ErroInterno",
                    Mensagem = "Erro interno durante o processamento do login"
                });
            }

            // ✅ AGORA podemos acessar usuarioAtualizado.UsuarioPapeis com segurança
            var papeis = usuarioAtualizado.UsuarioPapeis
                .Where(up => up.Ativo && up.Papel.Ativo)
                .Select(up => up.Papel.Name!)
                .ToList();

            var permissoes = await ObterPermissoesUsuario(usuarioAtualizado.Id);

            // Criar estatísticas usando usuarioAtualizado
            var estatisticas = new EstatisticasLogin
            {
                TotalLoginsSuccesso = usuarioAtualizado.ContadorLogins,
                TotalTentativasLogin = usuarioAtualizado.ContadorLogins + usuarioAtualizado.AccessFailedCount,
                TaxaSucessoLogin = usuarioAtualizado.ContadorLogins + usuarioAtualizado.AccessFailedCount > 0
                    ? (double)usuarioAtualizado.ContadorLogins / (usuarioAtualizado.ContadorLogins + usuarioAtualizado.AccessFailedCount) * 100
                    : 100,
                PrimeiroLogin = usuarioAtualizado.DataCriacao,
                TempoDesdeUltimoLogin = usuarioAtualizado.UltimoLogin.HasValue
                    ? DateTime.UtcNow - usuarioAtualizado.UltimoLogin.Value
                    : null,
                MedialoginsPorMes = CalcularMediaLoginsPorMes(usuarioAtualizado),
                DiasDesdeRegistro = (DateTime.UtcNow - usuarioAtualizado.DataCriacao).Days
            };

            // Construir resposta usando usuarioAtualizado
            var respostaLogin = new RespostaLogin
            {
                Sucesso = true,
                Token = tokenData.GetProperty("access_token").GetString()!,
                TipoToken = tokenData.GetProperty("token_type").GetString()!,
                ExpiracaoEm = DateTimeOffset.UtcNow.AddSeconds(tokenData.GetProperty("expires_in").GetInt32()).DateTime,
                RefreshToken = tokenData.TryGetProperty("refresh_token", out var rt) ? rt.GetString()! : "",
                Usuario = new InformacoesUsuario
                {
                    Id = usuarioAtualizado.Id,
                    Email = usuarioAtualizado.Email!,
                    Nome = usuarioAtualizado.Nome,
                    Sobrenome = usuarioAtualizado.Sobrenome,
                    NomeCompleto = usuarioAtualizado.NomeCompleto ?? $"{usuarioAtualizado.Nome} {usuarioAtualizado.Sobrenome}",
                    UltimoLogin = usuarioAtualizado.UltimoLogin,
                    ContadorLogins = usuarioAtualizado.ContadorLogins,
                    UltimaTentativaLogin = usuarioAtualizado.UltimaTentativaLogin,
                    TentativasLoginFalha = usuarioAtualizado.AccessFailedCount,
                    Papeis = papeis,
                    Permissoes = permissoes,
                    InformacoesTimezone = CriarInformacoesTimezoneUsuario(usuarioAtualizado.PreferenciaTimezone),
                    EstatisticasLogin = estatisticas
                }
            };

            _logger.LogInformation("✅ Login realizado com sucesso para: {Email} - ContadorLogins: {ContadorLogins}",
                request.Email, usuarioAtualizado.ContadorLogins);

            return Ok(respostaLogin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro durante o login para: {Email}", request.Email ?? "email_nulo");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno durante o login"
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
            var userId = User.FindFirstValue(OpenIddictConstants.Claims.Subject);

            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userIdInt))
            {
                return Unauthorized(new RespostaErro
                {
                    Erro = "TokenInvalido",
                    Mensagem = "Token inválido ou expirado"
                });
            }

            var usuario = await _userManager.Users
                .Include(u => u.UsuarioPapeis.Where(up => up.Ativo))
                    .ThenInclude(up => up.Papel)
                        .ThenInclude(p => p.PapelPermissoes.Where(pp => pp.Ativo))
                            .ThenInclude(pp => pp.Permissao)
                .FirstOrDefaultAsync(u => u.Id == userIdInt);

            if (usuario == null || !usuario.Ativo)
            {
                return Unauthorized(new RespostaErro
                {
                    Erro = "UsuarioInvalido",
                    Mensagem = "Usuário não encontrado ou inativo"
                });
            }

            // Obter papéis e permissões
            var papeis = usuario.UsuarioPapeis
                .Where(up => up.Ativo && up.Papel.Ativo)
                .Select(up => up.Papel.Name!)
                .ToList();

            var permissoes = usuario.UsuarioPapeis
                .Where(up => up.Ativo && up.Papel.Ativo)
                .SelectMany(up => up.Papel.PapelPermissoes)
                .Where(pp => pp.Ativo && pp.Permissao.Ativo)
                .Select(pp => pp.Permissao.Nome)
                .Distinct()
                .ToList();

            var informacoesUsuario = new InformacoesUsuario
            {
                Id = usuario.Id,
                Email = usuario.Email!,
                Nome = usuario.Nome,
                Sobrenome = usuario.Sobrenome,
                NomeCompleto = usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}",
                UltimoLogin = usuario.UltimoLogin,
                ContadorLogins = usuario.ContadorLogins,
                UltimaTentativaLogin = usuario.UltimaTentativaLogin,
                TentativasLoginFalha = usuario.AccessFailedCount,
                Papeis = papeis,
                Permissoes = permissoes,
                InformacoesTimezone = CriarInformacoesTimezoneUsuario(usuario.PreferenciaTimezone)
            };

            return Ok(informacoesUsuario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter perfil do usuário");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao obter perfil"
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

    /// <summary>
    /// Calcula média de logins por mês do usuário
    /// </summary>
    private double CalcularMediaLoginsPorMes(Usuario usuario)
    {
        var diasDesdeRegistro = (DateTime.UtcNow - usuario.DataCriacao).Days;
        if (diasDesdeRegistro == 0) return 0;

        var mesesDesdeRegistro = Math.Max(1, diasDesdeRegistro / 30.0);
        return Math.Round(usuario.ContadorLogins / mesesDesdeRegistro, 2);
    }

    /// <summary>
    /// Método auxiliar para criar informações de timezone do usuário:
    /// </summary>
    private object CriarInformacoesTimezoneUsuario(string? timezoneUsuario)
    {
        try
        {
            var timezoneId = timezoneUsuario ?? "America/Sao_Paulo";

            // Obter o timezone específico do usuário
            var userTimezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            var utcNow = DateTime.UtcNow;
            var localTimeUsuario = TimeZoneInfo.ConvertTimeFromUtc(utcNow, userTimezone);
            var offset = userTimezone.GetUtcOffset(localTimeUsuario);

            return new
            {
                TimezoneId = timezoneId,
                DisplayName = userTimezone.DisplayName,
                HorarioLocal = localTimeUsuario.ToString("dd/MM/yyyy HH:mm:ss"),
                HorarioUtc = utcNow.ToString("dd/MM/yyyy HH:mm:ss"),
                Offset = offset,
                OffsetString = offset.TotalHours >= 0 ? $"+{offset:hh\\:mm}" : $"{offset:hh\\:mm}",
                IsDaylightSaving = userTimezone.IsDaylightSavingTime(localTimeUsuario)
            };
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback para America/Sao_Paulo se o timezone do usuário não for válido
            var fallbackTimezone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            var utcNow = DateTime.UtcNow;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, fallbackTimezone);
            var offset = fallbackTimezone.GetUtcOffset(localTime);

            return new
            {
                TimezoneId = "America/Sao_Paulo",
                DisplayName = fallbackTimezone.DisplayName,
                HorarioLocal = localTime.ToString("dd/MM/yyyy HH:mm:ss"),
                HorarioUtc = utcNow.ToString("dd/MM/yyyy HH:mm:ss"),
                Offset = offset,
                OffsetString = offset.TotalHours >= 0 ? $"+{offset:hh\\:mm}" : $"{offset:hh\\:mm}",
                IsDaylightSaving = fallbackTimezone.IsDaylightSavingTime(localTime),
                Observacao = $"Timezone '{timezoneUsuario}' inválido, usando fallback"
            };
        }
        catch (Exception ex)
        {
            // Fallback para UTC em caso de erro geral
            _logger.LogError(ex, "Erro ao obter informações de timezone do usuário: {TimezoneUsuario}", timezoneUsuario);

            return new
            {
                TimezoneId = "UTC",
                DisplayName = "UTC",
                HorarioLocal = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"),
                HorarioUtc = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"),
                Offset = TimeSpan.Zero,
                OffsetString = "+00:00",
                IsDaylightSaving = false,
                Erro = "Erro ao processar timezone do usuário"
            };
        }
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
    /// Contador total de logins
    /// </summary>
    public int ContadorLogins { get; set; }

    /// <summary>
    /// Data/hora da última tentativa de login
    /// </summary>
    public DateTime? UltimaTentativaLogin { get; set; }

    /// <summary>
    /// Tentativas de login falharam
    /// </summary>
    public int TentativasLoginFalha { get; set; }

    /// <summary>
    /// Lista de papéis do usuário
    /// </summary>
    public List<string> Papeis { get; set; } = new();

    /// <summary>
    /// Lista de permissões do usuário
    /// </summary>
    public List<string> Permissoes { get; set; } = new();

    public object? InformacoesTimezone { get; set; }

    /// <summary>
    /// Estatísticas de sessão
    /// </summary>
    public EstatisticasLogin? EstatisticasLogin { get; set; }
}

/// <summary>
/// Estatísticas de login do usuário
/// </summary>
public class EstatisticasLogin
{
    /// <summary>
    /// Total de logins bem-sucedidos
    /// </summary>
    public int TotalLoginsSuccesso { get; set; }

    /// <summary>
    /// Total de tentativas de login (sucesso + falha)
    /// </summary>
    public int TotalTentativasLogin { get; set; }

    /// <summary>
    /// Taxa de sucesso do login (0-100)
    /// </summary>
    public double TaxaSucessoLogin { get; set; }

    /// <summary>
    /// Primeiro login registrado
    /// </summary>
    public DateTime? PrimeiroLogin { get; set; }

    /// <summary>
    /// Tempo desde o último login
    /// </summary>
    public TimeSpan? TempoDesdeUltimoLogin { get; set; }

    /// <summary>
    /// Média de logins por mês
    /// </summary>
    public double MedialoginsPorMes { get; set; }

    /// <summary>
    /// Dias desde a criação da conta
    /// </summary>
    public int DiasDesdeRegistro { get; set; }
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