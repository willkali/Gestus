namespace Gestus.Domain.ValueObjects;

/// <summary>
/// Classe base abstrata para Value Objects.
/// Value Objects são imutáveis e comparados por valor, não por identidade.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Obtém os componentes atômicos do Value Object para comparação.
    /// </summary>
    /// <returns>Enumeração dos componentes que definem a igualdade</returns>
    protected abstract IEnumerable<object?> ObterComponentesDeIgualdade();

    /// <summary>
    /// Determina se dois Value Objects são iguais.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;

        return ObterComponentesDeIgualdade()
            .SequenceEqual(other.ObterComponentesDeIgualdade());
    }

    /// <summary>
    /// Retorna o hash code do Value Object baseado em seus componentes.
    /// </summary>
    public override int GetHashCode()
    {
        return ObterComponentesDeIgualdade()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    /// <summary>
    /// Operador de igualdade.
    /// </summary>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// Operador de desigualdade.
    /// </summary>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}
