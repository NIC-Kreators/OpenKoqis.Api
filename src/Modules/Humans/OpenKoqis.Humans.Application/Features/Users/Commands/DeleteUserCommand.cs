using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using OpenKoqis.Humans.Application.Features.Users.Errors;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;

namespace OpenKoqis.Humans.Application.Features.Users.Commands;

public class DeleteUserCommandRequest : ICommand<ErrorOr<Deleted>>
{
    public required Guid Id { get; init; }
}

/// <summary>
/// Soft-deletes a user and disables its identity, which also ends its sessions. If the user is not saved, an
/// identity disabled by this call is enabled again. An identity that no longer exists in the provider does not
/// block the deletion.
/// </summary>
public class DeleteUserCommandHandler(
    IHumansUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IIdentityProvider identityProvider,
    ILogger<DeleteUserCommandHandler> logger) : ICommandHandler<DeleteUserCommandRequest, ErrorOr<Deleted>>
{
    public async ValueTask<ErrorOr<Deleted>> Handle(DeleteUserCommandRequest request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetUserWithId(request.Id, cancellationToken);

        if (user is null)
            return UserFeatureErrors.NotFound(request.Id);

        var deleted = user.Delete();

        if (deleted.IsError)
            return deleted.Errors;

        var isDisabledByThisCall = false;
        var isCommitted = false;

        try
        {
            var identity = await identityProvider.FindById(user.IdentityId, cancellationToken);

            if (identity is { IsEnabled: true })
            {
                var disabled = await identityProvider.SetEnabled(identity.Id, false, cancellationToken);

                if (disabled.IsError)
                    return disabled.Errors;

                isDisabledByThisCall = true;
            }

            var saved = await unitOfWork.SaveChangesAsync(cancellationToken);

            if (saved.IsError)
                return saved.Errors;

            isCommitted = true;

            return Result.Deleted;
        }
        finally
        {
            if (isDisabledByThisCall && !isCommitted)
                await EnableIdentityAsync(user.IdentityId);
        }
    }

    /// <summary>
    /// Enables the identity again after a deletion that was not saved. Uses <see cref="CancellationToken.None"/> so it
    /// completes even when the request was cancelled, and never throws, so it cannot replace the original failure.
    /// </summary>
    private async Task EnableIdentityAsync(string identityId)
    {
        try
        {
            var enabled = await identityProvider.SetEnabled(identityId, true, CancellationToken.None);

            if (enabled.IsError)
                logger.LogError(
                    "Failed to enable identity {IdentityId} again after user deletion failed; it stays disabled in the identity provider. Errors: {Errors}",
                    identityId,
                    enabled.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to enable identity {IdentityId} again after user deletion failed; it stays disabled in the identity provider",
                identityId);
        }
    }
}
