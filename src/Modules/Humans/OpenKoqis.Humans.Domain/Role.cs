using System.Diagnostics;
using ErrorOr;
using OpenKoqis.Humans.Domain.Errors;
using OpenKoqis.Shared.Kernel;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Humans.Domain;

[DebuggerDisplay("{Name}")]
public class Role : Entity<Guid>
{
    public record CreationAttributes
    {
        public required string Name { get; init; }
        public required IEnumerable<Access> Accesses { get; init; }
        public required Guid CreatedBy { get; init; }
    }

    private HashSet<Access> _accesses = [];

    public string Name { get; private set; } = null!;
    public IReadOnlySet<Access> Accesses => _accesses;
    public required Guid CreatedBy { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private Role() : base(Guid.CreateVersion7()) { }

    public static ErrorOr<Role> Create(CreationAttributes attributes)
    {
        if (attributes.CreatedBy == Guid.Empty)
            return RoleErrors.CouldntDetermineCreator;

        var trimmed = attributes.Name.Trim();

        if (!NameRules.IsValid(trimmed))
            return RoleErrors.InvalidName;

        var accessSet = attributes.Accesses as HashSet<Access> ?? [.. attributes.Accesses];

        if (accessSet.Count == 0)
            return RoleErrors.AtLeastOneAccessShouldBeDefined;

        return new Role
        {
            Name = trimmed,
            CreatedBy = attributes.CreatedBy,
            _accesses = accessSet
        };
    }

    public ErrorOr<Updated> Rename(string name)
    {
        var trimmed = name.Trim();

        if (!NameRules.IsValid(trimmed))
            return RoleErrors.InvalidName;

        Name = name;
        UpdatedAt = DateTime.UtcNow;

        return Result.Updated;
    }

    public ErrorOr<Updated> GrantAccess(IEnumerable<Access> accesses)
    {
        foreach (var access in accesses)
            _accesses.Add(access);

        UpdatedAt = DateTime.UtcNow;

        return Result.Updated;
    }

    public ErrorOr<Updated> RevokeAccess(IEnumerable<Access> accesses)
    {
        foreach (var access in accesses)
            _accesses.Remove(access);

        UpdatedAt = DateTime.UtcNow;

        return Result.Updated;
    }
}
