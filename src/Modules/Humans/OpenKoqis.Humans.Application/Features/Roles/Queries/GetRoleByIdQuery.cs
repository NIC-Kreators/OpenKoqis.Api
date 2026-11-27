using ErrorOr;
using Mediator;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Roles.Errors;

#pragma warning disable CS9113 // Handle is not implemented yet, so the injected dependencies are unread.

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
    public ValueTask<ErrorOr<RoleSummaryDto>> Handle(GetRoleByIdQueryRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
