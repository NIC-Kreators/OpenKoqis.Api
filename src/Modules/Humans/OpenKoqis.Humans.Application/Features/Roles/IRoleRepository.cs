using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Domain.Roles;

namespace OpenKoqis.Humans.Application.Features.Roles;

/// <summary>
/// Persisted roles of the Humans module. Roles returned by this repository are tracked: changes made to them, and roles
/// added or removed through it, are persisted when <see cref="IHumansUnitOfWork"/> saves changes. Query results come
/// from <see cref="IRoleReadStore"/> instead.
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Returns the role with the given <paramref name="id"/>; <c>null</c> when there is none.
    /// </summary>
    Task<Role?> GetRoleWithId(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Starts tracking a new role; it is persisted when <see cref="IHumansUnitOfWork"/> saves changes.
    /// </summary>
    void Add(Role role);

    /// <summary>
    /// Marks <paramref name="role"/> for deletion; it is deleted when <see cref="IHumansUnitOfWork"/> saves changes.
    /// </summary>
    void Remove(Role role);
}
