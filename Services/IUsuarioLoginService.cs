namespace Gestus.Services;

/// <summary>
/// Interface para o serviço de login de usuário
/// </summary>
public interface IUsuarioLoginService
{
    /// <summary>
    /// Registra uma tentativa de login
    /// </summary>
    Task RegistrarTentativaLoginAsync(string email);

    /// <summary>
    /// Registra um login bem-sucedido
    /// </summary>
    Task RegistrarLoginSucessoAsync(string email);
}