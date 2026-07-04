using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;
using OpenKoqis.Domain.Models.Dto;

namespace OpenKoqis.Application.Features.Users.Commands;

public record LoginUserCommand(string Nickname, string Password) : IRequest<ErrorOr<TokenPair>>;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, ErrorOr<TokenPair>>
{
    private readonly IMongoCollection<User> _collection;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<LoginUserCommandHandler> _logger;

    public LoginUserCommandHandler(
        IMongoDatabase database,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        ILogger<LoginUserCommandHandler> logger)
    {
        _collection = database.GetCollection<User>("Users");
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<ErrorOr<TokenPair>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for Nickname: {Nickname}", request.Nickname);

        var user = await _collection.Find(u => u.Nickname == request.Nickname).FirstOrDefaultAsync(cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Login failed. User {Nickname} not found", request.Nickname);
            return UserErrors.InvalidCredentials;
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed. Incorrect password for user {Nickname}", request.Nickname);
            return UserErrors.InvalidCredentials;
        }

        _logger.LogInformation("User {Nickname} logged in successfully", request.Nickname);

        var tokenPair = await _jwtService.GenerateTokenPairAsync(user.Id, user.Nickname, user.Role);
        return tokenPair;
    }
}
