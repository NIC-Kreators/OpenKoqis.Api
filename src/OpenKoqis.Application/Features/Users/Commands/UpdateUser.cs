using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Users.Commands;

public record UpdateUserCommand(string Id, User User) : IRequest<ErrorOr<Updated>>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, ErrorOr<Updated>>
{
    private readonly IMongoCollection<User> _collection;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(IMongoDatabase database, ILogger<UpdateUserCommandHandler> logger)
    {
        _collection = database.GetCollection<User>("Users");
        _logger = logger;
    }

    public async Task<ErrorOr<Updated>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating user with ID: {UserId}", request.Id);

        var existing = await _collection.Find(u => u.Id == request.Id).FirstOrDefaultAsync(cancellationToken);
        if (existing == null)
        {
            _logger.LogWarning("Update failed. User '{UserId}' not found", request.Id);
            return UserErrors.NotFound(request.Id);
        }

        var user = request.User;
        user.Id = existing.Id;
        user.CreatedAt = existing.CreatedAt;
        user.UpdatedAt = DateTime.UtcNow;

        await _collection.ReplaceOneAsync(u => u.Id == request.Id, user, cancellationToken: cancellationToken);
        _logger.LogInformation("User {UserId} successfully updated", request.Id);

        return Result.Updated;
    }
}
