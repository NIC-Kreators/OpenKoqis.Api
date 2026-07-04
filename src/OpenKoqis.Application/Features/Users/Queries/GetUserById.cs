using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Users.Queries;

public record GetUserByIdQuery(string Id) : IRequest<ErrorOr<User>>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, ErrorOr<User>>
{
    private readonly IMongoCollection<User> _collection;
    private readonly ILogger<GetUserByIdQueryHandler> _logger;

    public GetUserByIdQueryHandler(IMongoDatabase database, ILogger<GetUserByIdQueryHandler> logger)
    {
        _collection = database.GetCollection<User>("Users");
        _logger = logger;
    }

    public async Task<ErrorOr<User>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching for user with ID: {UserId}", request.Id);

        var user = await _collection.Find(u => u.Id == request.Id).FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User with ID: {UserId} not found", request.Id);
            return UserErrors.NotFound(request.Id);
        }

        _logger.LogInformation("User {UserId} found", request.Id);
        return user;
    }
}
