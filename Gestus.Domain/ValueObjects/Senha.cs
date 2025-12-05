using System.Security.Cryptography;
using System.Text;

namespace Gestus.Domain.ValueObjects;

/// <summary>
/// Value Object que representa uma senha com hash e validação de complexidade.
/// Garante que apenas senhas seguras sejam aceitas no sistema.
/// </summary>
public sealed class Senha : ValueObject
{
    /// <summary>
    /// Tamanho mínimo da senha.
    /// </summary>
    public const int TAMANHO_MINIMO = 8;

    /// <summary>
    /// Hash da senha (PBKDF2).
    /// </summary>
    public string Hash { get; }

    /// <summary>
    /// Cria uma nova instância de Senha a partir de texto claro.
    /// </summary>
    /// <param name="senhaTextoClaro">Senha em texto claro</param>
    /// <exception cref="ArgumentException">Quando a senha não atende aos requisitos</exception>
    public Senha(string senhaTextoClaro)
    {
        if (string.IsNullOrWhiteSpace(senhaTextoClaro))
        {
            throw new ArgumentException("Senha não pode ser vazia ou nula", nameof(senhaTextoClaro));
        }

        // Validar complexidade
        if (!EhValida(senhaTextoClaro))
        {
            throw new ArgumentException(
                $"Senha não atende aos requisitos de complexidade. " +
                $"Deve ter no mínimo {TAMANHO_MINIMO} caracteres, " +
                $"incluindo: maiúscula, minúscula, número e caractere especial.",
                nameof(senhaTextoClaro));
        }

        // Gerar hash
        Hash = GerarHash(senhaTextoClaro);
    }

    /// <summary>
    /// Cria uma instância de Senha a partir de um hash existente.
    /// Usado ao carregar do banco de dados.
    /// </summary>
    /// <param name="hash">Hash da senha</param>
    /// <param name="deHash">Flag indicando que é um hash (para diferenciação)</param>
    private Senha(string hash, bool deHash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new ArgumentException("Hash não pode ser vazio ou nulo", nameof(hash));
        }

        Hash = hash;
    }

    /// <summary>
    /// Cria uma instância de Senha a partir de um hash existente.
    /// </summary>
    /// <param name="hash">Hash da senha armazenado no banco</param>
    /// <returns>Instância de Senha com o hash fornecido</returns>
    public static Senha DeHash(string hash)
    {
        return new Senha(hash, deHash: true);
    }

    /// <summary>
    /// Verifica se uma senha em texto claro corresponde a este hash.
    /// </summary>
    /// <param name="senhaTextoClaro">Senha em texto claro para verificar</param>
    /// <returns>True se a senha corresponde ao hash, False caso contrário</returns>
    public bool Verificar(string senhaTextoClaro)
    {
        if (string.IsNullOrWhiteSpace(senhaTextoClaro))
        {
            return false;
        }

        try
        {
            // Decodificar o hash armazenado
            var hashCompleto = Convert.FromBase64String(Hash);

            // Extrair salt (primeiros 16 bytes)
            var salt = new byte[16];
            Array.Copy(hashCompleto, 0, salt, 0, 16);

            // Extrair hash (restante dos bytes)
            var hashArmazenado = new byte[hashCompleto.Length - 16];
            Array.Copy(hashCompleto, 16, hashArmazenado, 0, hashArmazenado.Length);

            // Gerar hash da senha fornecida com o mesmo salt
            const int iteracoes = 100000;
            var hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(senhaTextoClaro),
                salt,
                iteracoes,
                HashAlgorithmName.SHA256,
                hashArmazenado.Length);

            // Comparar os hashes
            return hashArmazenado.SequenceEqual(hashCalculado);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Valida se uma senha atende aos requisitos de complexidade.
    /// </summary>
    /// <param name="senha">Senha a ser validada</param>
    /// <returns>True se a senha é válida, False caso contrário</returns>
    private static bool EhValida(string senha)
    {
        if (senha.Length < TAMANHO_MINIMO)
        {
            return false;
        }

        // Verificar se tem pelo menos uma letra maiúscula
        if (!senha.Any(char.IsUpper))
        {
            return false;
        }

        // Verificar se tem pelo menos uma letra minúscula
        if (!senha.Any(char.IsLower))
        {
            return false;
        }

        // Verificar se tem pelo menos um número
        if (!senha.Any(char.IsDigit))
        {
            return false;
        }

        // Verificar se tem pelo menos um caractere especial
        if (!senha.Any(c => !char.IsLetterOrDigit(c)))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gera o hash PBKDF2 de uma senha.
    /// </summary>
    /// <param name="senha">Senha em texto claro</param>
    /// <returns>Hash da senha em formato Base64</returns>
    private static string GerarHash(string senha)
    {
        // Usar PBKDF2 com 100.000 iterações
        const int iteracoes = 100000;
        const int tamanhoHash = 32; // 256 bits

        // Gerar salt aleatório
        var salt = RandomNumberGenerator.GetBytes(16);

        // Gerar hash
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(senha),
            salt,
            iteracoes,
            HashAlgorithmName.SHA256,
            tamanhoHash);

        // Combinar salt + hash e converter para Base64
        var hashCompleto = new byte[salt.Length + hash.Length];
        Array.Copy(salt, 0, hashCompleto, 0, salt.Length);
        Array.Copy(hash, 0, hashCompleto, salt.Length, hash.Length);

        return Convert.ToBase64String(hashCompleto);
    }

    /// <summary>
    /// Retorna uma representação mascarada da senha (para logs).
    /// </summary>
    public override string ToString() => "********";

    /// <summary>
    /// Obtém os componentes para comparação de igualdade.
    /// </summary>
    protected override IEnumerable<object?> ObterComponentesDeIgualdade()
    {
        yield return Hash;
    }
}
