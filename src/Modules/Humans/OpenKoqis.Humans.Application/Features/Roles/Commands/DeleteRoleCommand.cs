using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using OpenKoqis.Humans.Application.Features.Roles.Errors;
using OpenKoqis.Humans.Application.Features.Users;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Users;

namespace OpenKoqis.Humans.Application.Features.Roles.Commands;

public class DeleteRoleCommandRequest : ICommand<ErrorOr<Deleted>>
{
    public required Guid Id { get; init; }

    /// <summary>
    /// Deletes the role even when active users are assigned to it, unassigning them. Without it, such a deletion
    /// fails with <see cref="RoleFeatureErrors.HasAssignedUsers"/>.
    /// </summary>
    public bool Force { get; init; }
}

/// <summary>
/// Deletes a role. Active users assigned to it block the deletion unless it is forced, in which case they are
/// unassigned in the same save and keep their dedicated access. Soft-deleted users keep the role id; restoring them
/// unassigns it. The projection of the role is removed through <see cref="IAccessProjection"/> before the save; when
/// that fails, nothing is saved and <see cref="RoleFeatureErrors.AccessProjectionFailed"/> is returned. When anything
/// after the projection fails to commit, the projection of the role is restored; a failure of that restoration is
/// logged and does not replace the original outcome. Users that were assigned to the role then lack its access in the
/// identity provider until their access is projected again, which is logged with their ids.
/// </summary>
public class DeleteRoleCommandHandler(
    IHumansUnitOfWork unitOfWork,
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    IAccessProjection accessProjection,
    ILogger<DeleteRoleCommandHandler> logger) : ICommandHandler<DeleteRoleCommandRequest, ErrorOr<Deleted>>
{
    public async ValueTask<ErrorOr<Deleted>> Handle(DeleteRoleCommandRequest request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetRoleWithId(request.Id, cancellationToken);

        if (role is null)
            return RoleFeatureErrors.NotFound(request.Id);

        var users = await userRepository.GetActiveUsersWithRole(role.Id, cancellationToken);

        if (users.Count > 0 && !request.Force)
            return RoleFeatureErrors.HasAssignedUsers(role.Id, users.Count);

        if (!await TryRemoveProjectionAsync(role.Id, cancellationToken))
            return RoleFeatureErrors.AccessProjectionFailed(role.Id);

        var isCommitted = false;

        try
        {
            foreach (var user in users)
            {
                var unassigned = user.UpdateRole(null);

                if (unassigned.IsError)
                    return unassigned.Errors;
            }

            roleRepository.Remove(role);

            var saved = await unitOfWork.SaveChangesAsync(cancellationToken);

            if (saved.IsError)
                return saved.Errors;

            isCommitted = true;

            return Result.Deleted;
        }
        finally
        {
            if (!isCommitted)
                await RestoreProjectionAsync(role, users);
        }
    }

    /// <summary>
    /// Removes the projection of the role. Returns <c>false</c>, after logging the cause, when the projection returns
    /// errors or throws.
    /// </summary>
    private async Task<bool> TryRemoveProjectionAsync(Guid roleId, CancellationToken cancellationToken)
    {
        try
        {
            var removed = await accessProjection.RoleDeleted(roleId, cancellationToken);

            if (!removed.IsError)
                return true;

            logger.LogError(
                "Failed to remove the projection of role {RoleId} while deleting it. Errors: {Errors}",
                roleId,
                removed.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove the projection of role {RoleId} while deleting it", roleId);
        }

        return false;
    }

    /// <summary>
    /// Projects the role again after a deletion that was not saved. Uses <see cref="CancellationToken.None"/> so it
    /// completes even when the request was cancelled, and never throws, so it cannot replace the original failure.
    /// </summary>
    private async Task RestoreProjectionAsync(Role role, IReadOnlyList<User> users)
    {
        if (users.Count > 0)
            logger.LogWarning(
                "Deletion of role {RoleId} was not saved after its projection was removed. Users {UserIds} lost the access of the role in the identity provider until their access is projected again",
                role.Id,
                users.Select(u => u.Id).ToArray());

        try
        {
            var restored = await accessProjection.RoleChanged(role, CancellationToken.None);

            if (restored.IsError)
                logger.LogError(
                    "Failed to restore the projection of role {RoleId} after its deletion was not saved; the role stays unprojected in the identity provider. Errors: {Errors}",
                    role.Id,
                    restored.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to restore the projection of role {RoleId} after its deletion was not saved; the role stays unprojected in the identity provider",
                role.Id);
        }
    }
}
