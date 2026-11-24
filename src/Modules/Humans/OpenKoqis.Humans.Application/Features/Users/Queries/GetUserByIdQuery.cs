using ErrorOr;
using Mediator;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Users.Errors;

namespace OpenKoqis.Humans.Application.Features.Users.Queries;

public class GetUserByIdQueryRequest : IQuery<ErrorOr<UserDto>>
{
    public required Guid Id { get; init; }
}

/// <summary>
/// Returns the user with the given id, soft-deleted users included.
/// </summary>
public class GetUserByIdQueryHandler(
    IUserReadStore userReadStore) : IQueryHandler<GetUserByIdQueryRequest, ErrorOr<UserDto>>
{
    public async ValueTask<ErrorOr<UserDto>> Handle(GetUserByIdQueryRequest request, CancellationToken cancellationToken)
    {
        var user = await userReadStore.GetUserById(request.Id, cancellationToken);

        if (user is null)
            return UserFeatureErrors.NotFound(request.Id);

        return user;
    }
}
