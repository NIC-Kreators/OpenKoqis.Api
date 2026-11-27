using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Shared.Application.Dto;

namespace OpenKoqis.Humans.Application.Features.Roles;

/// <summary>
/// Read-only view of roles, shaped for query results.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Only query handlers use it. Command handlers load and decide on aggregates through
/// <see cref="IRoleRepository"/>.</item>
/// <item>It returns DTOs, never domain entities, and nothing it returns is tracked.</item>
/// <item>It reads only the Humans module's own data.</item>
/// <item>It does not validate. Callers validate and normalize their input; the store applies it as given.</item>
/// <item><see cref="RoleSummaryDto.UsersCount"/> counts active users only.</item>
/// </list>
/// </remarks>
public interface IRoleReadStore
{
    /// <summary>
    /// Returns a page of the roles that match <paramref name="filter"/>.
    /// </summary>
    Task<PaginatedResponseDto<RoleSummaryDto>> GetRoles(RoleFilter filter, PaginatedRequestDto pageRequest = default, CancellationToken ct = default);

    /// <summary>
    /// Returns the role with the given <paramref name="id"/>; <c>null</c> when there is none.
    /// </summary>
    Task<RoleSummaryDto?> GetRoleById(Guid id, CancellationToken ct = default);
}
