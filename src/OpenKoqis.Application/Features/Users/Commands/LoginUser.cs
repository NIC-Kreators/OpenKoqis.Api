using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Application.Features.Users.Errors;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Users.Commands;

public record LoginUserCommand(string Nickname, string Password) : IRequest<ErrorOr<TokenPair>>;

public class LoginUserCommandHandler(IMongoDatabase database, IJwtService jwtService, IPasswordHasher passwordHasher, ILogger<LoginUserCommandHandler> logger) : IRequestHandler<LoginUserCommand, ErrorOr<TokenPair>>
{
    private readonly IMongoCollection<User> _collection = database.GetCollection<User>("Users");

    public async ValueTask<ErrorOr<TokenPair>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        if (await _collection.Find(u => u.Nickname.Value == request.Nickname).FirstOrDefaultAsync(cancellationToken) is not { } user ||
            !passwordHasher.VerifyPassword(request.Password, user.PasswordHash.Value))
            return UserErrors.InvalidCredentials;

        logger.LogInformation("User {Nickname} logged in successfully", request.Nickname);
        return await jwtService.GenerateTokenPairAsync(user.Id, user.Nickname.Value, user.Role);
    }
}
