namespace OpenKoqis.Shared.Application.Services;

/// <summary>
/// Named locks shared between all instances of the application. Every acquisition gets its own owner token,
/// and only that token can release the lock, so an owner whose lock expired cannot release a later owner's lock.
/// </summary>
/// <remarks>
/// Failures of the underlying store are thrown, never reported as a held lock.
/// </remarks>
public interface ILockManager
{
    /// <summary>
    /// Tries to take the lock <paramref name="key"/>. When <paramref name="ttl"/> is <c>null</c>, the implementation's
    /// default lifetime applies. Returns the owner token of the new acquisition, or <c>null</c> when the lock is
    /// already held.
    /// </summary>
    Task<Guid?> AcquireLockAsync(string key, TimeSpan? ttl = null, CancellationToken ct = default);

    /// <summary>
    /// Releases the lock <paramref name="key"/> if it is still held under <paramref name="ownerToken"/>.
    /// Returns <c>false</c> when it is not, e.g. because it expired and may have been taken by someone else;
    /// the lock is left untouched then.
    /// </summary>
    Task<bool> ReleaseLockAsync(string key, Guid ownerToken, CancellationToken ct = default);
}
