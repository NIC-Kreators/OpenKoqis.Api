namespace OpenKoqis.Domain.Shared;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj) =>
        obj is not null && obj.GetType() == GetType() && Equals((ValueObject)obj);

    public bool Equals(ValueObject? other) =>
        other is not null && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override int GetHashCode() =>
        GetEqualityComponents().Select(x => x?.GetHashCode() ?? 0).Aggregate((x, y) => x ^ y);

    public static bool operator ==(ValueObject? left, ValueObject? right) => EqualOperator(left, right);
    public static bool operator !=(ValueObject? left, ValueObject? right) => !EqualOperator(left, right);

    private static bool EqualOperator(ValueObject? left, ValueObject? right) =>
        ReferenceEquals(left, right) || (left is not null && right is not null && left.Equals(right));
}
