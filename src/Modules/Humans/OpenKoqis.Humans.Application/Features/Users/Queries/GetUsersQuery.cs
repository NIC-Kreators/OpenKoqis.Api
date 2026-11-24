using ErrorOr;
using Mediator;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Domain.Users.Errors;
using OpenKoqis.Shared.Application.Dto;

namespace OpenKoqis.Humans.Application.Features.Users.Queries;

public class GetUsersQueryRequest : IQuery<ErrorOr<PaginatedResponseDto<UserDto>>>
{
    /// <summary>
    /// Text the full name, login, email or phone number contains; <c>null</c>, empty or whitespace matches every user.
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Returns only the users assigned to this role; <c>null</c> matches every user.
    /// </summary>
    public Guid? RoleId { get; init; }

    /// <summary>
    /// <c>false</c> returns active users, <c>true</c> only soft-deleted ones.
    /// </summary>
    public bool IsDeleted { get; init; }

    public uint Page { get; init; } = 1;
    public uint PageSize { get; init; } = PaginatedRequestDto.DefaultPageSize;
}

/// <summary>
/// Returns a page of users matching the request's filters.
/// </summary>
public class GetUsersQueryHandler(
    IUserReadStore userReadStore) : IQueryHandler<GetUsersQueryRequest, ErrorOr<PaginatedResponseDto<UserDto>>>
{
    public async ValueTask<ErrorOr<PaginatedResponseDto<UserDto>>> Handle(GetUsersQueryRequest request, CancellationToken cancellationToken)
    {
        var pageRequest = PaginatedRequestDto.Create(request.Page, request.PageSize);

        List<Error> inputErrors = [.. pageRequest.ErrorsOrEmptyList];

        if (request.RoleId == Guid.Empty)
            inputErrors.Add(UserErrors.EmptyRoleId);

        if (inputErrors.Count is not 0)
            return inputErrors;

        var search = request.Search?.Trim();

        var filter = new UserFilter
        {
            Search = string.IsNullOrEmpty(search) ? null : search,
            RoleId = request.RoleId,
            IsDeleted = request.IsDeleted,
        };

        return await userReadStore.GetUsers(filter, pageRequest.Value, cancellationToken);
    }
}
