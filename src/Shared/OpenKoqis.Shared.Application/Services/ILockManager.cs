namespace OpenKoqis.Shared.Application.Services;

/// <summary>
/// Named locks shared between all instances of the application.
/// </summary>
public interface ILockManager
{
    /// <summary>
    /// Tries to take the lock <paramref name="key"/>. When <paramref name="ttl"/> is <c>null</c>, the implementation's
    /// default lifetime applies. Returns <c>false</c> when the lock is already held.
    /// </summary>
    Task<bool> AcquireLockAsync(string key, TimeSpan? ttl = null, CancellationToken ct = default);

    /// <summary>
    /// Releases the lock <paramref name="key"/>. Returns <c>false</c> when it was not held, e.g. because it expired.
    /// </summary>
    Task<bool> ReleaseLockAsync(string key, CancellationToken ct = default);
}
