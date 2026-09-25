using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Shared.Kernel.Access;

/// <summary>
/// A protected resource together with the permissions that can be granted on it,
/// against which an <see cref="Access"/> is checked.
/// </summary>
public sealed class Resource : ValueObject
{
    /// <summary>
    /// The resource name, e.g. <c>bins</c>. Must satisfy <see cref="SystemNameRules"/> with a maximum length of 32.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The permissions that may be granted on this resource. Each must satisfy <see cref="SystemNameRules"/>.
    /// </summary>
    public required IReadOnlySet<string> AllowedPermissions { get; init; }

    /// <summary>
    /// Creates a resource, validating <paramref name="name"/> and <paramref name="allowedPermissions"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="allowedPermissions"/> is empty.</exception>
    /// <exception cref="ArgumentException">The name or a permission has an invalid format.</exception>
    public Resource(string name, IEnumerable<string> allowedPermissions)
    {
        var trimmed = name.ToLowerInvariant().Trim();
        var permissionSet = allowedPermissions.ToHashSet();

        ArgumentOutOfRangeException.ThrowIfLessThan(permissionSet.Count, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!permissionSet.All(p => SystemNameRules.IsValid(p)) || !SystemNameRules.IsValid(trimmed, maxLength: 32))
            throw new ArgumentException("Invalid permission name format");

        Name = name;
        AllowedPermissions = permissionSet;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;

        var sortedPermissions = AllowedPermissions.OrderBy(p => p);

        foreach (var permission in sortedPermissions)
            yield return permission;
    }
}
