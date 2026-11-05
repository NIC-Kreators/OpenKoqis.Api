using Microsoft.Extensions.Logging;

namespace OpenKoqis.Shared.Application.Services;

public static class LockManagerExtensions
{
    /// <summary>
    /// Tries to take the lock <paramref name="key"/>. Returns a lease that releases it on dispose,
    /// or <c>null</c> when the lock is held by someone else. Failures of the lock store are thrown.
    /// </summary>
    public static async Task<LockLease?> TryLeaseLockAsync(
        this ILockManager lockManager,
        string key,
        TimeSpan? ttl = null,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        var ownerToken = await lockManager.AcquireLockAsync(key, ttl, ct);

        if (!ownerToken.HasValue)
        {
            logger?.LogInformation("Lock {Key} is already held by another owner", key);
            return null;
        }

        return new LockLease(lockManager, key, ownerToken.Value, logger);
    }
}
