namespace Gestus.Services;

/// <summary>
/// Interface para serviço de autenticação OpenIddict
/// </summary>
public interface IOpenIddictAuthService
{
    /// <summary>
    /// Autentica o usuário via OpenIddict
    /// </summary>
    Task<AuthResult> AuthenticateAsync(string username, string password, string clientId, string clientSecret, string scope);

    /// <summary>
    /// Renova o token via OpenIddict
    /// </summary>
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string clientId, string clientSecret);

    /// <summary>
    /// Valida um token via introspecção
    /// </summary>
    Task<IntrospectionResult> IntrospectTokenAsync(string token, string tokenTypeHint, string clientId, string clientSecret);
}

/// <summary>
/// Resultado da autenticação
/// </summary>
public class AuthResult
{
    public bool IsSuccessful { get; set; }
    public string? AccessToken { get; set; }
    public string? TokenType { get; set; }
    public int ExpiresIn { get; set; }
    public string? RefreshToken { get; set; }
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
}

/// <summary>
/// Resultado da introspecção de token
/// </summary>
public class IntrospectionResult
{
    public bool IsActive { get; set; }
    public string? Subject { get; set; }
    public string? Error { get; set; }
}