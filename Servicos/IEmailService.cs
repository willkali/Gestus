namespace Gestus.Services;

public interface IEmailService
{
    Task<bool> EnviarEmailAsync(string destinatario, string assunto, string corpo, bool isHtml = true);
    Task<bool> EnviarEmailRecuperacaoSenhaAsync(string emailDestino, string nomeUsuario, string token);
    Task<bool> EnviarEmailBoasVindasAsync(string emailDestino, string nomeUsuario);
    Task<bool> EnviarEmailConfirmacaoAsync(string emailDestino, string nomeUsuario, string token);
    Task<bool> TestarConfiguracaoAsync(string emailDestino);
    Task<string> GerarCorpoEmailAsync(string tipoTemplate, Dictionary<string, string> variaveis);
}