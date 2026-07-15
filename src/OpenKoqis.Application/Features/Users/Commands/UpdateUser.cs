using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Users.Commands;

public record UpdateUserCommand(string Id, User User) : IRequest<ErrorOr<Updated>>;

public class UpdateUserCommandHandler(IMongoDatabase database, ILogger<UpdateUserCommandHandler> logger) : IRequestHandler<UpdateUserCommand, ErrorOr<Updated>>
{
    private readonly IMongoCollection<User> _collection = database.GetCollection<User>("Users");

    public async Task<ErrorOr<Updated>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating user with ID: {UserId}", request.Id);

        var existing = await _collection.Find(u => u.Id == request.Id).FirstOrDefaultAsync(cancellationToken);
        if (existing is null)
        {
            logger.LogWarning("Update failed. User '{UserId}' not found", request.Id);
            return UserErrors.NotFound(request.Id);
        }

        var user = request.User;
        user.Id = existing.Id;
        user.CreatedAt = existing.CreatedAt;
        user.UpdatedAt = DateTime.UtcNow;

        await _collection.ReplaceOneAsync(u => u.Id == request.Id, user, cancellationToken: cancellationToken);
        logger.LogInformation("User {UserId} successfully updated", request.Id);

        return Result.Updated;
    }
}
