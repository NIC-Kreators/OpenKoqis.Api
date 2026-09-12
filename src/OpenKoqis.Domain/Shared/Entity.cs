namespace OpenKoqis.Domain.Shared;

public abstract class Entity<TId>(TId id) : IEquatable<Entity<TId>> where TId : IEquatable<TId>
{
    public TId Id { get; init; } = id;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    protected void MarkModified() => UpdatedAt = DateTime.UtcNow;

    public bool Equals(Entity<TId>? other) =>
        other is not null && (ReferenceEquals(this, other) || other.Id.Equals(Id));

    public override bool Equals(object? obj) =>
        obj is not null && (ReferenceEquals(this, obj) || (obj.GetType() == GetType() && Equals((Entity<TId>)obj)));

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
