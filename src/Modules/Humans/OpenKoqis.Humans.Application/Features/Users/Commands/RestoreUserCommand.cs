using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Roles;
using OpenKoqis.Humans.Application.Features.Users.Errors;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Humans.Application.Services.Errors;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Users;

namespace OpenKoqis.Humans.Application.Features.Users.Commands;

public class RestoreUserCommandRequest : ICommand<ErrorOr<RestoreUserCommandResponse>>
{
    public required Guid Id { get; init; }
}

public record RestoreUserCommandResponse
{
    public required UserDto User { get; init; }
}

/// <summary>
/// Brings back a soft-deleted user: enables its identity again when it is disabled, saves the user, then pushes its
/// access through <see cref="IAccessProjection"/>. A role deleted while the user was deleted is unassigned. If the
/// user is not saved, an identity enabled by this call is disabled again.
/// </summary>
public class RestoreUserCommandHandler(
    IHumansUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IIdentityProvider identityProvider,
    IAccessProjection accessProjection,
    ILogger<RestoreUserCommandHandler> logger) : ICommandHandler<RestoreUserCommandRequest, ErrorOr<RestoreUserCommandResponse>>
{
    public async ValueTask<ErrorOr<RestoreUserCommandResponse>> Handle(RestoreUserCommandRequest request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetUserWithIdIncludingDeleted(request.Id, cancellationToken);

        if (user is null)
            return UserFeatureErrors.NotFound(request.Id);

        var restored = user.Restore();

        if (restored.IsError)
            return restored.Errors;

        Role? role = null;

        if (user.RoleId is { } roleId)
        {
            role = await roleRepository.GetRoleWithId(roleId, cancellationToken);

            if (role is null)
            {
                var unassigned = user.UpdateRole(null);

                if (unassigned.IsError)
                    return unassigned.Errors;
            }
        }

        var isEnabledByThisCall = false;
        var isCommitted = false;

        try
        {
            var identity = await identityProvider.FindById(user.IdentityId, cancellationToken);

            if (identity is null)
                return IdentityErrors.NotFoundById(user.IdentityId);

            if (!identity.IsEnabled)
            {
                var enabled = await identityProvider.SetEnabled(identity.Id, true, cancellationToken);

                if (enabled.IsError)
                    return enabled.Errors;

                isEnabledByThisCall = true;
            }

            var saved = await unitOfWork.SaveChangesAsync(cancellationToken);

            if (saved.IsError)
                return saved.Errors;

            isCommitted = true;
        }
        finally
        {
            if (isEnabledByThisCall && !isCommitted)
                await DisableIdentityAsync(user.IdentityId);
        }

        if (!await TryProjectAsync(user, role, cancellationToken))
            return UserFeatureErrors.AccessProjectionFailed(user.Id);

        return new RestoreUserCommandResponse { User = UserDto.FromUser(user, role) };
    }

    /// <summary>
    /// Disables the identity again after a restore that was not saved. Uses <see cref="CancellationToken.None"/> so it
    /// completes even when the request was cancelled, and never throws, so it cannot replace the original failure.
    /// </summary>
    private async Task DisableIdentityAsync(string identityId)
    {
        try
        {
            var disabled = await identityProvider.SetEnabled(identityId, false, CancellationToken.None);

            if (disabled.IsError)
                logger.LogError(
                    "Failed to disable identity {IdentityId} again after user restoration failed; it stays enabled in the identity provider. Errors: {Errors}",
                    identityId,
                    disabled.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to disable identity {IdentityId} again after user restoration failed; it stays enabled in the identity provider",
                identityId);
        }
    }

    private async Task<bool> TryProjectAsync(User user, Role? role, CancellationToken cancellationToken)
    {
        try
        {
            var projected = await accessProjection.UserAccessChanged(user, role, cancellationToken);

            if (!projected.IsError)
                return true;

            logger.LogError(
                "User {UserId} was restored, but projecting its access failed. Errors: {Errors}",
                user.Id,
                projected.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "User {UserId} was restored, but projecting its access threw", user.Id);
        }

        return false;
    }
}
