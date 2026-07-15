using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Users.Commands;

public record CreateUserCommand(User User) : IRequest<ErrorOr<User>>;

public class CreateUserCommandHandler(IMongoDatabase database, ILogger<CreateUserCommandHandler> logger) : IRequestHandler<CreateUserCommand, ErrorOr<User>>
{
    private readonly IMongoCollection<User> _collection = database.GetCollection<User>("Users");

    public async Task<ErrorOr<User>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating new user with Nickname: {Nickname}", request.User.Nickname);

        var user = request.User;
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = user.CreatedAt;

        await _collection.InsertOneAsync(user, cancellationToken: cancellationToken);
        logger.LogInformation("User {Nickname} inserted into database", user.Nickname);

        return user;
    }
}
