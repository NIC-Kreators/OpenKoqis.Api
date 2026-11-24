using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Shared.Application.Dto;

namespace OpenKoqis.Humans.Application.Features.Users;

/// <summary>
/// Read-only view of users, shaped for query results.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Only query handlers use it. Command handlers load and decide on aggregates through
/// <see cref="IUserRepository"/>.</item>
/// <item>It returns DTOs, never domain entities, and nothing it returns is tracked.</item>
/// <item>It reads only the Humans module's own data. Data owned by another module comes through that module's public
/// contract.</item>
/// <item>It does not validate. Callers validate and normalize their input; the store applies it as given.</item>
/// <item>A role id that points to no role yields <see cref="UserDto.RoleName"/> <c>null</c>.</item>
/// </list>
/// </remarks>
public interface IUserReadStore
{
    /// <summary>
    /// Returns a page of the users that match <paramref name="filter"/>.
    /// </summary>
    Task<PaginatedResponseDto<UserDto>> GetUsers(UserFilter filter, PaginatedRequestDto pageRequest = default, CancellationToken ct = default);

    /// <summary>
    /// Returns the user with the given <paramref name="id"/>, soft-deleted or not; <c>null</c> when there is none.
    /// </summary>
    Task<UserDto?> GetUserById(Guid id, CancellationToken ct = default);
}
