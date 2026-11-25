using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Shared.Application.Dto;

namespace OpenKoqis.Humans.Application.Features.Roles;

/// <summary>
/// Persisted roles of the Humans module. Roles returned by this repository are tracked: changes made to them, and roles
/// added or removed through it, are persisted when <see cref="IHumansUnitOfWork"/> saves changes.
/// </summary>
public interface IRoleRepository
{
    Task<PaginatedResponseDto<Role>> SearchRolesByName(string name, PaginatedRequestDto paginatedRequest = default, CancellationToken ct = default);
    Task<PaginatedResponseDto<Role>> GetPaginatedRoles(PaginatedRequestDto pageRequest = default, CancellationToken ct = default);

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
