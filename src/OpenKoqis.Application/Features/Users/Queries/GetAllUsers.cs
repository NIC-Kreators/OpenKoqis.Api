using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Users.Queries;

public record GetAllUsersQuery : IRequest<ErrorOr<List<User>>>;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, ErrorOr<List<User>>>
{
    private readonly IMongoCollection<User> _collection;
    private readonly ILogger<GetAllUsersQueryHandler> _logger;

    public GetAllUsersQueryHandler(IMongoDatabase database, ILogger<GetAllUsersQueryHandler> logger)
    {
        _collection = database.GetCollection<User>("Users");
        _logger = logger;
    }

    public async Task<ErrorOr<List<User>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching all users from repository");

        var users = await _collection.Find(FilterDefinition<User>.Empty).ToListAsync(cancellationToken);

        _logger.LogInformation("Successfully retrieved {Count} users", users.Count);
        return users;
    }
}
