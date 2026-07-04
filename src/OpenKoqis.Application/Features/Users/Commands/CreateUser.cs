using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Users.Commands;

public record CreateUserCommand(User User) : IRequest<ErrorOr<User>>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, ErrorOr<User>>
{
    private readonly IMongoCollection<User> _collection;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(IMongoDatabase database, ILogger<CreateUserCommandHandler> logger)
    {
        _collection = database.GetCollection<User>("Users");
        _logger = logger;
    }

    public async Task<ErrorOr<User>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new user with Nickname: {Nickname}", request.User.Nickname);

        var user = request.User;
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = user.CreatedAt;

        await _collection.InsertOneAsync(user, cancellationToken: cancellationToken);
        _logger.LogInformation("User {Nickname} inserted into database", user.Nickname);

        return user;
    }
}
