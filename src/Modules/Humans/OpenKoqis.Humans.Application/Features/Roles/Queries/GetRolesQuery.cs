using ErrorOr;
using Mediator;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Shared.Application.Dto;

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
    public async ValueTask<ErrorOr<PaginatedResponseDto<RoleSummaryDto>>> Handle(GetRolesQueryRequest request, CancellationToken cancellationToken)
    {
        var pageRequest = PaginatedRequestDto.Create(request.Page, request.PageSize);

        if (pageRequest.IsError)
            return pageRequest.Errors;

        var search = request.Search?.Trim();

        var filter = new RoleFilter
        {
            Search = string.IsNullOrEmpty(search) ? null : search,
        };

        return await roleReadStore.GetRoles(filter, pageRequest.Value, cancellationToken);
    }
}
