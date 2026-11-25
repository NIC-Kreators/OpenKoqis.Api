using ErrorOr;

namespace OpenKoqis.Humans.Application.Features.Roles.Errors;

/// <summary>
/// Errors produced by the role management use cases.
/// </summary>
public static class RoleFeatureErrors
{
    public const string RoleIdMetadataKey = "RoleId";
    public const string UsersCountMetadataKey = "UsersCount";

    public static Error NotFound(Guid id) => Error.NotFound(
        code: "Role.NotFound",
        description: $"No role with id '{id}' exists.");

    /// <summary>
    /// The role is assigned to active users, and the deletion was not forced. The role id is in the metadata under
    /// <see cref="RoleIdMetadataKey"/>, the number of those users under <see cref="UsersCountMetadataKey"/>.
    /// </summary>
    public static Error HasAssignedUsers(Guid roleId, int usersCount) => Error.Conflict(
        code: "Role.HasAssignedUsers",
        description: $"Role '{roleId}' is assigned to {usersCount} user(s). Deleting it unassigns them; force the deletion to proceed.",
        metadata: new Dictionary<string, object>
        {
            [RoleIdMetadataKey] = roleId,
            [UsersCountMetadataKey] = usersCount,
        });

    /// <summary>
    /// The role change could not be pushed to the identity provider. Nothing was saved, so the request can be
    /// repeated. The role id is in the metadata under <see cref="RoleIdMetadataKey"/>.
    /// </summary>
    public static Error AccessProjectionFailed(Guid roleId) => Error.Failure(
        code: "Role.AccessProjectionFailed",
        description: $"The change of role '{roleId}' was not applied to the identity provider and nothing was saved. Repeat the request.",
        metadata: new Dictionary<string, object> { [RoleIdMetadataKey] = roleId });
}
