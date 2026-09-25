using System.Diagnostics;
using ErrorOr;
using OpenKoqis.Humans.Domain.Roles.Errors;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Shared.Kernel;
using OpenKoqis.Shared.Kernel.Access;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Humans.Domain.Roles;

/// <summary>
/// A named, reusable set of <see cref="Access"/> entries that can be assigned to users.
/// </summary>
[DebuggerDisplay("{Name}")]
public class Role : Entity<Guid>
{
    /// <summary>
    /// Input for <see cref="Create"/>.
    /// </summary>
    public record CreationAttributes
    {
        public required string Name { get; init; }
        public required IEnumerable<Access> Accesses { get; init; }

        /// <summary>
        /// Id of the <see cref="User"/> creating the role.
        /// </summary>
        public required Guid CreatedBy { get; init; }
    }

    private HashSet<Access> _accesses = [];

    public string Name { get; private set; } = null!;
    public IReadOnlySet<Access> Accesses => _accesses;

    /// <summary>
    /// Id of the <see cref="User"/> who created the role.
    /// </summary>
    public required Guid CreatedBy { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private Role() : base(Guid.CreateVersion7()) { }

    /// <summary>
    /// Creates a role, requiring a known creator, a valid name and at least one access.
    /// All validation errors are returned together.
    /// </summary>
    public static ErrorOr<Role> Create(CreationAttributes attributes)
    {
        List<Error> errors = [];

        if (attributes.CreatedBy == Guid.Empty)
            errors.Add(RoleErrors.CouldntDetermineCreator);

        var trimmed = attributes.Name.Trim();

        if (!NameRules.IsValid(trimmed))
            errors.Add(RoleErrors.InvalidName);

        var accessSet = attributes.Accesses.ToHashSet();

        if (accessSet.Count == 0)
            errors.Add(RoleErrors.AtLeastOneAccessShouldBeDefined);

        if (errors.Count > 0)
            return errors;

        return new Role
        {
            Name = trimmed,
            CreatedBy = attributes.CreatedBy,
            _accesses = accessSet
        };
    }

    /// <summary>
    /// Replaces the role's name with the trimmed <paramref name="name"/>.
    /// </summary>
    public ErrorOr<Updated> Rename(string name)
    {
        var trimmed = name.Trim();

        if (!NameRules.IsValid(trimmed))
            return RoleErrors.InvalidName;

        Name = trimmed;
        UpdatedAt = DateTime.UtcNow;

        return Result.Updated;
    }

    /// <summary>
    /// Adds <paramref name="accesses"/> to the role; entries already present are ignored.
    /// </summary>
    public ErrorOr<Updated> GrantAccess(IEnumerable<Access> accesses)
    {
        foreach (var access in accesses)
            _accesses.Add(access);

        UpdatedAt = DateTime.UtcNow;

        return Result.Updated;
    }

    /// <summary>
    /// Removes <paramref name="accesses"/> from the role. An entry is removed only when both
    /// its resource and its full permission set match an existing one.
    /// </summary>
    public ErrorOr<Updated> RevokeAccess(IEnumerable<Access> accesses)
    {
        foreach (var access in accesses)
            _accesses.Remove(access);

        UpdatedAt = DateTime.UtcNow;

        return Result.Updated;
    }
}
