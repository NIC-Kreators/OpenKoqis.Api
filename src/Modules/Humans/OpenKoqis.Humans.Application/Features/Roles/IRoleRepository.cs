using ErrorOr;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Shared.Application.Dto;

namespace OpenKoqis.Humans.Application.Features.Roles;

public interface IRoleRepository
{
    Task<PaginatedResponseDto<Role>> SearchRolesByName(string name, PaginatedRequestDto paginatedRequest = default, CancellationToken ct = default);
    Task<PaginatedResponseDto<Role>> GetPaginatedRoles(PaginatedRequestDto pageRequest = default, CancellationToken ct = default);
    Task<Role?> GetRoleWithId(Guid id, CancellationToken ct = default);

    Task<ErrorOr<Created>> CreateRole(Role role, CancellationToken ct = default);

    Task<ErrorOr<Updated>> UpdateRole(Role role, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeleteRoleWithId(Guid id, CancellationToken ct = default);
}
