using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Gestus.Dados;
using Gestus.Modelos;

namespace Gestus.Services;

/// <summary>
/// Serviço para gerenciar eventos de login e auditar tentativas
/// </summary>
public class UsuarioLoginService : IUsuarioLoginService
{
    private readonly UserManager<Usuario> _userManager;
    private readonly GestusDbContexto _context;
    private readonly ILogger<UsuarioLoginService> _logger;

    public UsuarioLoginService(
        UserManager<Usuario> userManager,
        GestusDbContexto context,
        ILogger<UsuarioLoginService> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Registra uma tentativa de login
    /// </summary>
    public async Task RegistrarTentativaLoginAsync(string email)
    {
        if (string.IsNullOrEmpty(email)) return;

        try
        {
            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario == null) return;

            // Atualizar apenas UltimaTentativaLogin
            usuario.UltimaTentativaLogin = DateTime.UtcNow;
            
            var resultado = await _userManager.UpdateAsync(usuario);
            if (resultado.Succeeded)
            {
                _logger.LogInformation("✅ Tentativa de login registrada para {Email} em {DataTentativa}", 
                    email, usuario.UltimaTentativaLogin);
            }
            else
            {
                _logger.LogWarning("⚠️ Erro ao registrar tentativa de login para {Email}: {Errors}",
                    email, string.Join(", ", resultado.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao registrar tentativa de login para {Email}", email);
        }
    }

    /// <summary>
    /// Registra um login bem-sucedido
    /// </summary>
    public async Task RegistrarLoginSucessoAsync(string email)
    {
        if (string.IsNullOrEmpty(email)) return;

        try
        {
            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario == null || !usuario.Ativo) return;

            // Incrementar contador e atualizar último login
            usuario.ContadorLogins += 1;
            usuario.UltimoLogin = DateTime.UtcNow;
            
            var resultado = await _userManager.UpdateAsync(usuario);
            if (resultado.Succeeded)
            {
                _logger.LogInformation("✅ Login bem-sucedido registrado para {Email} - ContadorLogins: {ContadorLogins}", 
                    email, usuario.ContadorLogins);
            }
            else
            {
                _logger.LogWarning("⚠️ Erro ao registrar login bem-sucedido para {Email}: {Errors}",
                    email, string.Join(", ", resultado.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao registrar login bem-sucedido para {Email}", email);
        }
    }
}