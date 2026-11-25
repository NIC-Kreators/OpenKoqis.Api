using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using OpenKoqis.Humans.Application.Features.Roles.Errors;
using OpenKoqis.Humans.Application.Features.Users;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;

#pragma warning disable CS9113 // Handle is not implemented yet, so the injected dependencies are unread.

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
/// that fails, nothing is saved and <see cref="RoleFeatureErrors.AccessProjectionFailed"/> is returned. When the save
/// fails, the projection of the role is restored.
/// </summary>
public class DeleteRoleCommandHandler(
    IHumansUnitOfWork unitOfWork,
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    IAccessProjection accessProjection,
    ILogger<DeleteRoleCommandHandler> logger) : ICommandHandler<DeleteRoleCommandRequest, ErrorOr<Deleted>>
{
    public ValueTask<ErrorOr<Deleted>> Handle(DeleteRoleCommandRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
