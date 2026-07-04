using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Users.Commands;

public record DeleteUserCommand(string Id) : IRequest<ErrorOr<Deleted>>;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, ErrorOr<Deleted>>
{
    private readonly IMongoCollection<User> _collection;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(IMongoDatabase database, ILogger<DeleteUserCommandHandler> logger)
    {
        _collection = database.GetCollection<User>("Users");
        _logger = logger;
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to delete user with ID: {UserId}", request.Id);

        var result = await _collection.DeleteOneAsync(u => u.Id == request.Id, cancellationToken);

        if (result.DeletedCount == 0)
        {
            _logger.LogWarning("Delete failed. User '{UserId}' not found", request.Id);
            return UserErrors.NotFound(request.Id);
        }

        _logger.LogInformation("User {UserId} deleted from database", request.Id);
        return Result.Deleted;
    }
}
