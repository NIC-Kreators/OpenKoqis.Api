using System.Diagnostics;
using ErrorOr;
using OpenKoqis.Shared.Kernel.Errors;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Shared.Kernel.Access;

/// <summary>
/// A set of permissions granted on a single resource, written as <c>resource:permission,permission</c>.
/// Resource and permission names are stored lowercased and trimmed.
/// </summary>
[DebuggerDisplay("{FriendlyName}")]
public class Access : ValueObject
{
    /// <summary>
    /// Property created specially for <see cref="DebuggerDisplayAttribute"/>.
    /// </summary>
    private string FriendlyName => $"{Resource}:{string.Join(',', Permissions)}";

    /// <summary>
    /// The resource the permissions apply to, e.g. <c>bins</c>.
    /// </summary>
    public required string Resource { get; init; }

    /// <summary>
    /// The permissions granted on <see cref="Resource"/>, e.g. <c>read</c> or <c>write</c>.
    /// </summary>
    public required IReadOnlySet<string> Permissions { get; init; }

    private Access() { }

    /// <summary>
    /// Parses an access string of the form <c>resource:permission,permission</c>.
    /// </summary>
    public static ErrorOr<Access> Parse(string raw)
    {
        var resourceAndPermissions = raw.Split(':');

        if (resourceAndPermissions.Length != 2)
            return AccessErrors.InvalidAccessString(raw);

        var resource = resourceAndPermissions[0];
        var permissions = resourceAndPermissions[1]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (string.IsNullOrWhiteSpace(resource) || permissions.Length == 0)
            return AccessErrors.InvalidAccessString(raw);

        return Parse(resource, permissions);
    }

    /// <summary>
    /// Builds an access from an already separated <paramref name="resource"/> and <paramref name="permissions"/>,
    /// normalizing both and collecting every invalid name as a separate error.
    /// </summary>
    public static ErrorOr<Access> Parse(string resource, IEnumerable<string> permissions)
    {
        var trimmed = resource.ToLowerInvariant().Trim();

        IReadOnlySet<string> permissionSet = permissions
            .Select(p => p.ToLowerInvariant().Trim())
            .ToHashSet();

        List<Error> errors =
        [
            .. permissionSet
                .Where(p => !SystemNameRules.IsValid(p))
                .Select(p => AccessErrors.InvalidPermission(p))
        ];

        if (!SystemNameRules.IsValid(trimmed, maxLength: 32))
            errors.Add(AccessErrors.InvalidResource(trimmed));

        if (errors.Count > 0)
            return errors;

        return new Access
        {
            Resource = trimmed,
            Permissions = permissionSet,
        };
    }

    /// <summary>
    /// Combines <paramref name="accesses"/> into one access per resource, uniting their permissions.
    /// </summary>
    public static IReadOnlySet<Access> Merge(IEnumerable<Access> accesses) =>
        accesses
            .GroupBy(a => a.Resource)
            .Select(g => new Access
            {
                Resource = g.Key,
                Permissions = g.SelectMany(a => a.Permissions).ToHashSet(),
            })
            .ToHashSet();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Resource;

        var sortedPermissions = Permissions.OrderBy(p => p);

        foreach (var permission in sortedPermissions)
            yield return permission;
    }
}
