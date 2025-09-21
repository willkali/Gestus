using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Gestus.Dados;
using Gestus.Modelos;

namespace Gestus.Services;

public class EmailService : IEmailService
{
    private readonly GestusDbContexto _context;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IChaveVersaoService _chaveVersaoService; // ✅ MUDANÇA

    public EmailService(
        GestusDbContexto context,
        ILogger<EmailService> logger,
        IConfiguration configuration,
        IChaveVersaoService chaveVersaoService) // ✅ MUDANÇA
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _chaveVersaoService = chaveVersaoService; // ✅ MUDANÇA
    }

    public async Task<bool> EnviarEmailAsync(string destinatario, string assunto, string corpo, bool isHtml = true)
    {
        try
        {
            var configuracao = await ObterConfiguracaoEmailAsync();
            if (configuracao == null)
            {
                _logger.LogError("❌ Nenhuma configuração de email encontrada");
                return false;
            }

            using var client = new SmtpClient(configuracao.ServidorSmtp, configuracao.Porta);
            
            if (configuracao.UsarAutenticacao)
            {
                var senhaDescriptografada = await DescriptografarSenhaAsync(configuracao.SenhaEncriptada);
                client.Credentials = new NetworkCredential(configuracao.EmailRemetente, senhaDescriptografada);
            }

            client.EnableSsl = configuracao.UsarSsl;
            
            var message = new MailMessage
            {
                From = new MailAddress(configuracao.EmailRemetente, configuracao.NomeRemetente),
                Subject = assunto,
                Body = corpo,
                IsBodyHtml = isHtml
            };

            message.To.Add(destinatario);

            // Adicionar email de resposta se configurado
            if (!string.IsNullOrEmpty(configuracao.EmailResposta))
            {
                message.ReplyToList.Add(configuracao.EmailResposta);
            }

            // Adicionar cópia se configurado
            if (!string.IsNullOrEmpty(configuracao.EmailCopia))
            {
                message.CC.Add(configuracao.EmailCopia);
            }

            await client.SendMailAsync(message);
            
            _logger.LogInformation("✅ Email enviado com sucesso para: {Destinatario}", destinatario);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao enviar email para: {Destinatario}", destinatario);
            return false;
        }
    }

    public async Task<bool> EnviarEmailRecuperacaoSenhaAsync(string emailDestino, string nomeUsuario, string token)
    {
        try
        {
            var template = await ObterTemplateAsync("RecuperarSenha");
            if (template == null)
            {
                _logger.LogError("❌ Template 'RecuperarSenha' não encontrado");
                return false;
            }

            var variaveis = new Dictionary<string, string>
            {
                { "NomeUsuario", nomeUsuario },
                { "EmailUsuario", emailDestino },
                { "Token", token },
                { "LinkRecuperacao", $"{_configuration["App:BaseUrl"]}/recuperar-senha?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(emailDestino)}" },
                { "DataExpiracao", DateTime.UtcNow.AddHours(24).ToString("dd/MM/yyyy HH:mm") },
                { "NomeSistema", "Gestus" }
            };

            var assunto = SubstituirVariaveis(template.Assunto, variaveis);
            var corpo = SubstituirVariaveis(template.CorpoHtml, variaveis);

            return await EnviarEmailAsync(emailDestino, assunto, corpo, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao enviar email de recuperação de senha para: {Email}", emailDestino);
            return false;
        }
    }

    public async Task<bool> EnviarEmailBoasVindasAsync(string emailDestino, string nomeUsuario)
    {
        try
        {
            var template = await ObterTemplateAsync("BoasVindas");
            if (template == null)
            {
                _logger.LogError("❌ Template 'BoasVindas' não encontrado");
                return false;
            }

            var variaveis = new Dictionary<string, string>
            {
                { "NomeUsuario", nomeUsuario },
                { "EmailUsuario", emailDestino },
                { "LinkLogin", $"{_configuration["App:BaseUrl"]}/login" },
                { "NomeSistema", "Gestus" }
            };

            var assunto = SubstituirVariaveis(template.Assunto, variaveis);
            var corpo = SubstituirVariaveis(template.CorpoHtml, variaveis);

            return await EnviarEmailAsync(emailDestino, assunto, corpo, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao enviar email de boas-vindas para: {Email}", emailDestino);
            return false;
        }
    }

    public async Task<bool> EnviarEmailConfirmacaoAsync(string emailDestino, string nomeUsuario, string token)
    {
        try
        {
            var template = await ObterTemplateAsync("ConfirmarEmail");
            if (template == null)
            {
                _logger.LogError("❌ Template 'ConfirmarEmail' não encontrado");
                return false;
            }

            var variaveis = new Dictionary<string, string>
            {
                { "NomeUsuario", nomeUsuario },
                { "EmailUsuario", emailDestino },
                { "Token", token },
                { "LinkConfirmacao", $"{_configuration["App:BaseUrl"]}/confirmar-email?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(emailDestino)}" },
                { "NomeSistema", "Gestus" }
            };

            var assunto = SubstituirVariaveis(template.Assunto, variaveis);
            var corpo = SubstituirVariaveis(template.CorpoHtml, variaveis);

            return await EnviarEmailAsync(emailDestino, assunto, corpo, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao enviar email de confirmação para: {Email}", emailDestino);
            return false;
        }
    }

    public async Task<bool> TestarConfiguracaoAsync(string emailDestino)
    {
        try
        {
            var assunto = $"[{DateTime.Now:dd/MM/yyyy HH:mm}] Teste de Configuração - Sistema Gestus";
            var corpo = $@"
                <h2>🎯 Teste de Configuração de Email</h2>
                <p><strong>Sistema:</strong> Gestus IAM</p>
                <p><strong>Data/Hora:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
                <p><strong>Status:</strong> ✅ Configuração funcionando corretamente!</p>
                <hr>
                <p><em>Este é um email de teste automático. Se você recebeu esta mensagem, 
                significa que as configurações de email do sistema estão funcionando corretamente.</em></p>
            ";

            return await EnviarEmailAsync(emailDestino, assunto, corpo, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro no teste de configuração de email para: {Email}", emailDestino);
            return false;
        }
    }

    public async Task<string> GerarCorpoEmailAsync(string tipoTemplate, Dictionary<string, string> variaveis)
    {
        var template = await ObterTemplateAsync(tipoTemplate);
        if (template == null)
        {
            return string.Empty;
        }

        return SubstituirVariaveis(template.CorpoHtml, variaveis);
    }

    private async Task<ConfiguracaoEmail?> ObterConfiguracaoEmailAsync()
    {
        return await _context.Set<ConfiguracaoEmail>()
            .Where(c => c.Ativo)
            .FirstOrDefaultAsync();
    }

    private async Task<TemplateEmail?> ObterTemplateAsync(string tipo)
    {
        return await _context.Set<TemplateEmail>()
            .Where(t => t.Tipo == tipo && t.Ativo)
            .Include(t => t.ConfiguracaoEmail)
            .FirstOrDefaultAsync();
    }

    private string SubstituirVariaveis(string template, Dictionary<string, string> variaveis)
    {
        var resultado = template;
        
        foreach (var variavel in variaveis)
        {
            resultado = resultado.Replace($"{{{variavel.Key}}}", variavel.Value);
        }

        return resultado;
    }

    private async Task<string> DescriptografarSenhaAsync(string senhaEncriptada)
    {
        try
        {
            return await _chaveVersaoService.DescriptografarComVersaoAsync(senhaEncriptada, "EmailPassword");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao descriptografar senha de email");
            throw;
        }
    }

    public async Task<string> EncriptarSenhaAsync(string senha)
    {
        try
        {
            return await _chaveVersaoService.EncriptarComVersaoAsync(senha, "EmailPassword");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao encriptar senha de email");
            throw;
        }
    }
}