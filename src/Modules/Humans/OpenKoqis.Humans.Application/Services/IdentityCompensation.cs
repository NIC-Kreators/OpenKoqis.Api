using Microsoft.Extensions.Logging;
using OpenKoqis.Humans.Application.Features.Users;

namespace OpenKoqis.Humans.Application.Services;

/// <summary>
/// Undoes identity provider side effects of a use case that did not save its user.
/// </summary>
internal static class IdentityCompensation
{
    /// <summary>
    /// Deletes <paramref name="identity"/>, created by a run of <paramref name="operation"/> that did not save its
    /// user, unless a persisted user is linked to it: the save may have committed before failing, or a concurrent run
    /// may have linked its user to the same identity, and deleting it then would leave a user that cannot sign in.
    /// Uses <see cref="CancellationToken.None"/> so it completes even when the request was cancelled, and never
    /// throws, so it cannot replace the original failure; failures are logged. <paramref name="operation"/> names the
    /// use case as it reads in a log message, e.g. <c>user creation</c>.
    /// </summary>
    public static async Task DeleteUnlessLinkedAsync(
        IIdentityProvider identityProvider,
        IUserRepository userRepository,
        Identity identity,
        string operation,
        ILogger logger)
    {
        try
        {
            if (await userRepository.IsIdentityLinked(identity.Id, CancellationToken.None))
            {
                logger.LogWarning(
                    "Kept identity {IdentityId} (login {Login}) after {Operation} failed: a persisted user is linked to it",
                    identity.Id,
                    identity.Login.Value,
                    operation);

                return;
            }

            var deleted = await identityProvider.Delete(identity.Id, CancellationToken.None);

            if (deleted.IsError)
                logger.LogError(
                    "Failed to delete identity {IdentityId} (login {Login}) after {Operation} failed; it is orphaned in the identity provider. Errors: {Errors}",
                    identity.Id,
                    identity.Login.Value,
                    operation,
                    deleted.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to delete identity {IdentityId} (login {Login}) after {Operation} failed; it is orphaned in the identity provider",
                identity.Id,
                identity.Login.Value,
                operation);
        }
    }
}
