using System.Diagnostics;
using ErrorOr;
using OpenKoqis.Humans.Domain.Errors;
using OpenKoqis.Shared.Kernel;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Humans.Domain;

[DebuggerDisplay("{FriendlyName}")]
public class Access : ValueObject
{
    /// <summary>
    /// Property created specially for <see cref="DebuggerDisplayAttribute"/>.
    /// </summary>
    private string FriendlyName => $"{Resource}:{string.Join(',', Permissions)}";

    public required string Resource { get; init; }
    public required IReadOnlySet<string> Permissions { get; init; }

    private Access() { }

    public static ErrorOr<Access> Parse(string raw)
    {
        var resourceAndPermissions = raw.Split(':');

        if (resourceAndPermissions.Length != 2)
            return AccessErrors.InvalidAccessString(raw);

        var resource = resourceAndPermissions[0];
        var permissions = resourceAndPermissions[1].Split(',');

        if (string.IsNullOrWhiteSpace(resource) || permissions.Any(string.IsNullOrWhiteSpace))
            return AccessErrors.InvalidAccessString(raw);

        return Parse(resource, permissions);
    }

    public static ErrorOr<Access> Parse(string resource, IEnumerable<string> permissions)
    {
        var trimmed = resource.ToLowerInvariant().Trim();

        IReadOnlySet<string> permissionSet = permissions
            .Select(p => p.ToLowerInvariant().Trim())
            .ToHashSet();

        if (!permissionSet.All(p => SystemNameRules.IsValid(p)))
            return AccessErrors.InvalidPermission(string.Join(", ", permissionSet));

        if (!SystemNameRules.IsValid(trimmed, maxLength: 32))
            return AccessErrors.InvalidResource(trimmed);

        return new Access
        {
            Resource = trimmed,
            Permissions = permissionSet,
        };
    }

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
