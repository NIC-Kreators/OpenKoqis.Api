using ErrorOr;
using Mediator;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Shared.Application.Dto;

#pragma warning disable CS9113 // Handle is not implemented yet, so the injected dependencies are unread.

namespace OpenKoqis.Humans.Application.Features.Users.Queries;

public class GetUsersQueryRequest : IQuery<ErrorOr<PaginatedResponseDto<UserDto>>>
{
    /// <summary>
    /// Part of the full name to filter by; <c>null</c>, empty or whitespace returns every user.
    /// </summary>
    public string? Name { get; init; }

    public uint Page { get; init; } = 1;
    public uint PageSize { get; init; } = PaginatedRequestDto.DefaultPageSize;
}

/// <summary>
/// Returns a page of users, optionally filtered by name. Soft-deleted users are not returned.
/// </summary>
public class GetUsersQueryHandler(
    IUserRepository userRepository) : IQueryHandler<GetUsersQueryRequest, ErrorOr<PaginatedResponseDto<UserDto>>>
{
    public ValueTask<ErrorOr<PaginatedResponseDto<UserDto>>> Handle(GetUsersQueryRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
