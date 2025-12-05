namespace Gestus.Domain.Enums;

/// <summary>
/// Define os tipos de autenticação suportados pelo sistema.
/// </summary>
public enum TipoAutenticacao
{
    /// <summary>
    /// Autenticação local com usuário e senha.
    /// </summary>
    Local = 1,

    /// <summary>
    /// Autenticação via Active Directory.
    /// </summary>
    ActiveDirectory = 2,

    /// <summary>
    /// Autenticação via OAuth 2.0 (Google, Microsoft, etc.).
    /// </summary>
    OAuth = 3,

    /// <summary>
    /// Autenticação via OpenID Connect.
    /// </summary>
    OpenIdConnect = 4,

    /// <summary>
    /// Autenticação via SAML 2.0.
    /// </summary>
    Saml = 5
}
