using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Users.Queries;

public record GetUserByIdQuery(string Id) : IRequest<ErrorOr<User>>;

public class GetUserByIdQueryHandler(IMongoDatabase database, ILogger<GetUserByIdQueryHandler> logger) : IRequestHandler<GetUserByIdQuery, ErrorOr<User>>
{
    private readonly IMongoCollection<User> _collection = database.GetCollection<User>("Users");

    public async Task<ErrorOr<User>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Searching for user with ID: {UserId}", request.Id);

        var user = await _collection.Find(u => u.Id == request.Id).FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            logger.LogWarning("User with ID: {UserId} not found", request.Id);
            return UserErrors.NotFound(request.Id);
        }

        logger.LogInformation("User {UserId} found", request.Id);
        return user;
    }
}
