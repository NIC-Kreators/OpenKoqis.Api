using Microsoft.Extensions.Logging;

namespace OpenKoqis.Shared.Application.Services;

/// <summary>
/// A held lock that is released when disposed, using the owner token of the acquisition that produced it.
/// Release failures are logged, never thrown.
/// </summary>
public sealed class LockLease(ILockManager lockManager, string key, Guid ownerToken, ILogger? logger) : IAsyncDisposable
{
    private bool _isReleased;

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isReleased, true))
            return;

        try
        {
            var isReleased = await lockManager.ReleaseLockAsync(key, ownerToken, CancellationToken.None);

            if (!isReleased)
                logger?.LogWarning("Lock {Key} was no longer held by this owner on release; it may have expired", key);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to release lock {Key}", key);
        }
    }
}
