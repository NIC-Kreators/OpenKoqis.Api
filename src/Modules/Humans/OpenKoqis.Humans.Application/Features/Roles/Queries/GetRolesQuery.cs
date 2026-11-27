using ErrorOr;
using Mediator;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Shared.Application.Dto;

#pragma warning disable CS9113 // Handle is not implemented yet, so the injected dependencies are unread.

namespace OpenKoqis.Humans.Application.Features.Roles.Queries;

public class GetRolesQueryRequest : IQuery<ErrorOr<PaginatedResponseDto<RoleSummaryDto>>>
{
    /// <summary>
    /// Text the role name contains; <c>null</c>, empty or whitespace matches every role.
    /// </summary>
    public string? Search { get; init; }

    public uint Page { get; init; } = 1;
    public uint PageSize { get; init; } = PaginatedRequestDto.DefaultPageSize;
}

/// <summary>
/// Returns a page of roles matching the request's search, ordered by name.
/// </summary>
public class GetRolesQueryHandler(
    IRoleReadStore roleReadStore) : IQueryHandler<GetRolesQueryRequest, ErrorOr<PaginatedResponseDto<RoleSummaryDto>>>
{
    public ValueTask<ErrorOr<PaginatedResponseDto<RoleSummaryDto>>> Handle(GetRolesQueryRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
