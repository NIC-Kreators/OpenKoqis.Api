namespace OpenKoqis.Shared.Kernel;

/// <summary>
/// Base Value Object class. <see href="https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects">Docs from Microsoft</see>
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

    protected abstract IEnumerable<object> GetEqualityComponents();

    public bool Equals(ValueObject? other)
        => other is not null && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        return Equals((ValueObject)obj);
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject one, ValueObject two) => EqualOperator(one, two);
    public static bool operator !=(ValueObject one, ValueObject two) => NotEqualOperator(one, two);
}
