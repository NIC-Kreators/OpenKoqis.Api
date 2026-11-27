namespace OpenKoqis.Humans.Application.Features.Roles;

/// <summary>
/// Which roles <see cref="IRoleReadStore.GetRoles"/> returns. Every set criterion must match; roles are ordered by
/// name, case-insensitively, with ties broken by id.
/// </summary>
public sealed record RoleFilter
{
    /// <summary>
    /// Text the name contains, case-insensitively; <c>null</c> matches every role.
    /// </summary>
    public string? Search { get; init; }
}
