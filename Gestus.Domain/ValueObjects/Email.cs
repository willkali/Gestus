using System.Text.RegularExpressions;

namespace Gestus.Domain.ValueObjects;

/// <summary>
/// Value Object que representa um endereço de email válido.
/// Garante que apenas emails válidos sejam criados no sistema.
/// </summary>
public sealed class Email : ValueObject
{
    /// <summary>
    /// Padrão regex para validação de email (RFC 5322 simplificado).
    /// </summary>
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Valor do email em formato normalizado (lowercase).
    /// </summary>
    public string Valor { get; }

    /// <summary>
    /// Cria uma nova instância de Email.
    /// </summary>
    /// <param name="valor">Endereço de email a ser validado</param>
    /// <exception cref="ArgumentException">Quando o email é inválido</exception>
    public Email(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException("Email não pode ser vazio ou nulo", nameof(valor));
        }

        // Normalizar para lowercase
        var emailNormalizado = valor.Trim().ToLowerInvariant();

        // Validar formato usando regex
        if (!EhValido(emailNormalizado))
        {
            throw new ArgumentException($"Email inválido: {valor}", nameof(valor));
        }

        Valor = emailNormalizado;
    }

    /// <summary>
    /// Verifica se um email é válido usando regex.
    /// </summary>
    /// <param name="email">Email a ser validado</param>
    /// <returns>True se o email é válido, False caso contrário</returns>
    private static bool EhValido(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        // Validação básica de formato
        return EmailRegex.IsMatch(email);
    }

    /// <summary>
    /// Retorna a representação em string do email.
    /// </summary>
    public override string ToString() => Valor;

    /// <summary>
    /// Conversão implícita de Email para string.
    /// </summary>
    public static implicit operator string(Email email) => email.Valor;

    /// <summary>
    /// Obtém os componentes para comparação de igualdade.
    /// </summary>
    protected override IEnumerable<object?> ObterComponentesDeIgualdade()
    {
        yield return Valor;
    }
}
