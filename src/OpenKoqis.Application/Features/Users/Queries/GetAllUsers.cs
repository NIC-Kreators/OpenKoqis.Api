using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Users.Queries;

public record GetAllUsersQuery : IRequest<ErrorOr<List<User>>>;

public class GetAllUsersQueryHandler(IMongoDatabase database, ILogger<GetAllUsersQueryHandler> logger) : IRequestHandler<GetAllUsersQuery, ErrorOr<List<User>>>
{
    private readonly IMongoCollection<User> _collection = database.GetCollection<User>("Users");

    public async Task<ErrorOr<List<User>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching all users from repository");

        var users = await _collection.Find(FilterDefinition<User>.Empty).ToListAsync(cancellationToken);

        logger.LogInformation("Successfully retrieved {Count} users", users.Count);
        return users;
    }
}
