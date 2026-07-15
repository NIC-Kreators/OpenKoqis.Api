using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Users.Commands;

public record DeleteUserCommand(string Id) : IRequest<ErrorOr<Deleted>>;

public class DeleteUserCommandHandler(IMongoDatabase database, ILogger<DeleteUserCommandHandler> logger) : IRequestHandler<DeleteUserCommand, ErrorOr<Deleted>>
{
    private readonly IMongoCollection<User> _collection = database.GetCollection<User>("Users");

    public async Task<ErrorOr<Deleted>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to delete user with ID: {UserId}", request.Id);

        var result = await _collection.DeleteOneAsync(u => u.Id == request.Id, cancellationToken);

        if (result.DeletedCount == 0)
        {
            logger.LogWarning("Delete failed. User '{UserId}' not found", request.Id);
            return UserErrors.NotFound(request.Id);
        }

        logger.LogInformation("User {UserId} deleted from database", request.Id);
        return Result.Deleted;
    }
}
