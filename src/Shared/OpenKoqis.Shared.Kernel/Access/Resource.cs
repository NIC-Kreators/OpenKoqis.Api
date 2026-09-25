using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Shared.Kernel.Access;

public sealed class Resource : ValueObject
{
    public required string Name { get; init; }
    public required IReadOnlySet<string> AllowedPermissions { get; init; }

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
