namespace Gestus.Domain.Enums;

/// <summary>
/// Define os tipos de aplicações que podem se autenticar no sistema.
/// </summary>
public enum TipoAplicacao
{
    /// <summary>
    /// Aplicação Web API (backend).
    /// </summary>
    WebApi = 1,

    /// <summary>
    /// Aplicação Web (frontend web).
    /// </summary>
    WebApp = 2,

    /// <summary>
    /// Aplicação Desktop (Windows, macOS, Linux).
    /// </summary>
    Desktop = 3,

    /// <summary>
    /// Aplicação Mobile (iOS, Android).
    /// </summary>
    Mobile = 4,

    /// <summary>
    /// Serviço ou daemon (processos em background).
    /// </summary>
    Servico = 5
}
