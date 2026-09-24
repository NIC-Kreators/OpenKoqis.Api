using ErrorOr;

namespace OpenKoqis.Humans.Domain.Errors;

/// <summary>
/// Errors produced by <see cref="User"/> invariants.
/// </summary>
public static class UserErrors
{
    public static Error InvalidIdentityId => Error.Validation(
        code: "User.InvalidIdentityId",
        description: "IdentityId should be between 1 and 2048 symbols.");

    public static Error EmptyRoleId => Error.Validation(
        code: "User.EmptyRoleId",
        description: "You cannot set the empty RoleId for User.");

    public static Error AtLeastOneAccessShouldBeDefined => Error.Validation(
        code: "User.AtLeastOneAccessShouldBeDefined",
        description: "You cannot create a user without any dedicated access.");

    public static Error RootAdminParallelCreation => Error.Conflict(
        code: "User.RootAdminParallelCreation",
        description: "You cannot create 2 root admins!");

    /// <summary>
    /// The role passed to a <see cref="User"/> operation is not the one the user is assigned to.
    /// </summary>
    public static Error RoleMismatch(string? userRole, string? requestedRole) => Error.Failure(
        code: "User.RoleMismatch",
        description: $"Role assigned to the user {userRole ?? "-"} doesn't match the requested role {requestedRole ?? "-"}");
}
