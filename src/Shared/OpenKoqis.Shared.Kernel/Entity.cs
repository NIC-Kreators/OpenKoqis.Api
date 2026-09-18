namespace OpenKoqis.Shared.Kernel;

public abstract class Entity<TId>(TId id) : IEquatable<Entity<TId>> where TId : IEquatable<TId>
{
    public TId Id { get; } = id;

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
            return false;

        return ReferenceEquals(this, other) || other.Id.Equals(Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;

        return obj.GetType() == GetType() && Equals((Entity<TId>)obj);
    }

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
