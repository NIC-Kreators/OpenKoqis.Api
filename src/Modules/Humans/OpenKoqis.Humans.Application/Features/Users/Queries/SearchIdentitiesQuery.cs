using ErrorOr;
using Mediator;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Shared.Application.Dto;

#pragma warning disable CS9113 // Handle is not implemented yet, so the injected dependencies are unread.

namespace OpenKoqis.Humans.Application.Features.Users.Queries;

public class SearchIdentitiesQueryRequest : IQuery<ErrorOr<PaginatedResponseDto<IdentityDto>>>
{
    /// <summary>
    /// Part of the login to search for; empty matches every identity.
    /// </summary>
    public required string Term { get; init; }

    public uint Page { get; init; } = 1;
    public uint PageSize { get; init; } = PaginatedRequestDto.DefaultPageSize;
}

/// <summary>
/// Returns a page of identity provider identities whose login contains the term, each flagged with whether a
/// user is already linked to it.
/// </summary>
public class SearchIdentitiesQueryHandler(
    IIdentityProvider identityProvider,
    IUserRepository userRepository) : IQueryHandler<SearchIdentitiesQueryRequest, ErrorOr<PaginatedResponseDto<IdentityDto>>>
{
    public ValueTask<ErrorOr<PaginatedResponseDto<IdentityDto>>> Handle(SearchIdentitiesQueryRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
