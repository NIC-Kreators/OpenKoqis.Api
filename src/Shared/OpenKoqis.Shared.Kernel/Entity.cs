namespace OpenKoqis.Shared.Kernel;

/// <summary>
/// Base class for entities: objects with identity. Two instances are equal when they are of the same runtime type
/// and have the same <see cref="Id"/>, regardless of their other state.
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
public abstract class Entity<TId>(TId id) : IEquatable<Entity<TId>> where TId : IEquatable<TId>
{
    /// <summary>
    /// The identifier of the entity; it does not change over the entity's lifetime.
    /// </summary>
    public TId Id { get; } = id;

    /// <summary>
    /// Compares the <see cref="Id"/> of this entity and <paramref name="other"/>.
    /// </summary>
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
            return false;

        return ReferenceEquals(this, other) || other.Id.Equals(Id);
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="obj"/> has the same runtime type and the same <see cref="Id"/>.
    /// </summary>
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
