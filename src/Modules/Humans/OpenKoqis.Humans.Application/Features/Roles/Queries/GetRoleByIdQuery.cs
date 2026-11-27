using ErrorOr;
using Mediator;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Roles.Errors;

namespace OpenKoqis.Humans.Application.Features.Roles.Queries;

public class GetRoleByIdQueryRequest : IQuery<ErrorOr<RoleSummaryDto>>
{
    public required Guid Id { get; init; }
}

/// <summary>
/// Returns the role with the given id, or <see cref="RoleFeatureErrors.NotFound"/> when there is none.
/// </summary>
public class GetRoleByIdQueryHandler(
    IRoleReadStore roleReadStore) : IQueryHandler<GetRoleByIdQueryRequest, ErrorOr<RoleSummaryDto>>
{
    public async ValueTask<ErrorOr<RoleSummaryDto>> Handle(GetRoleByIdQueryRequest request, CancellationToken cancellationToken)
    {
        var role = await roleReadStore.GetRoleById(request.Id, cancellationToken);

        if (role is null)
            return RoleFeatureErrors.NotFound(request.Id);

        return role;
    }
}
