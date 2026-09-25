namespace OpenKoqis.Shared.Kernel;

/// <summary>
/// Base class for value objects: objects without identity, compared by the values returned from
/// <see cref="GetEqualityComponents"/>. Two instances are equal only when they are of the same runtime type
/// and their components are equal in order.
/// <see href="https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects">Docs from Microsoft</see>
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    private static bool EqualOperator(ValueObject left, ValueObject right)
    {
        if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
        {
            return false;
        }

        return ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
    }

    private static bool NotEqualOperator(ValueObject left, ValueObject right) => !EqualOperator(left, right);

    /// <summary>
    /// Returns the values that define this object's equality and hash code, in a stable order.
    /// Must yield at least one component.
    /// </summary>
    protected abstract IEnumerable<object> GetEqualityComponents();

    /// <summary>
    /// Compares the equality components of this object and <paramref name="other"/>.
    /// </summary>
    public bool Equals(ValueObject? other)
        => other is not null && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="obj"/> has the same runtime type and equal components.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        return Equals((ValueObject)obj);
    }

    /// <summary>
    /// Combines the hash codes of the equality components; <see langword="null"/> components hash to zero.
    /// </summary>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject one, ValueObject two) => EqualOperator(one, two);
    public static bool operator !=(ValueObject one, ValueObject two) => NotEqualOperator(one, two);
}
