using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Users.Commands;

public record LoginUserCommand(string Nickname, string Password) : IRequest<ErrorOr<TokenPair>>;

public class LoginUserCommandHandler(IMongoDatabase database, IJwtService jwtService, IPasswordHasher passwordHasher, ILogger<LoginUserCommandHandler> logger) : IRequestHandler<LoginUserCommand, ErrorOr<TokenPair>>
{
    private readonly IMongoCollection<User> _collection = database.GetCollection<User>("Users");

    public async ValueTask<ErrorOr<TokenPair>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Login attempt for Nickname: {Nickname}", request.Nickname);

        var user = await _collection.Find(u => u.Nickname == request.Nickname).FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            logger.LogWarning("Login failed. User {Nickname} not found", request.Nickname);
            return UserErrors.InvalidCredentials;
        }

        if (!passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Login failed. Incorrect password for user {Nickname}", request.Nickname);
            return UserErrors.InvalidCredentials;
        }

        logger.LogInformation("User {Nickname} logged in successfully", request.Nickname);
        return await jwtService.GenerateTokenPairAsync(user.Id, user.Nickname, user.Role);
    }
}
