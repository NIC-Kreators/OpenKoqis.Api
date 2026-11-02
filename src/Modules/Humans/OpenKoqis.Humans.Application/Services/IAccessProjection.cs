using ErrorOr;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Humans.Domain.Users.Errors;

namespace OpenKoqis.Humans.Application.Services;

/// <summary>
/// Pushes authorization data to wherever tokens are issued, so that a token of a user carries the user's
/// effective access — the result of <see cref="User.ResolveFullAccess"/>. How that union is produced is up to
/// the implementation.
/// </summary>
/// <remarks>
/// Every call is idempotent: repeating it with the same state leaves the projection unchanged, so calls can be
/// retried after a failure. Changes reach tokens issued afterwards; tokens already issued keep their access
/// until they expire. The root user is never projected: its bypass is not carried by its token.
/// </remarks>
public interface IAccessProjection
{
    /// <summary>
    /// Creates or updates the projection of <paramref name="role"/> with its current accesses.
    /// Every user assigned to it is affected.
    /// </summary>
    Task<ErrorOr<Success>> RoleChanged(Role role, CancellationToken ct = default);

    /// <summary>
    /// Removes the projection of the role <paramref name="roleId"/>. Users still assigned to it lose its
    /// accesses. Removing a role that is not projected succeeds.
    /// </summary>
    Task<ErrorOr<Success>> RoleDeleted(Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates the projection of <paramref name="user"/>: its role assignment and dedicated access.
    /// <paramref name="role"/> must be the role the user is assigned to (or <c>null</c> when none is), otherwise
    /// <see cref="UserErrors.RoleMismatch"/> is returned. Does nothing for the root user.
    /// </summary>
    Task<ErrorOr<Success>> UserAccessChanged(User user, Role? role, CancellationToken ct = default);
}
