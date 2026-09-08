using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;
using OpenKoqis.Domain.Models.Dto;

namespace OpenKoqis.Application.Features.Users.Commands;

public record RegisterUserCommand(UserRegistrationDto RegistrationDto) : IRequest<ErrorOr<TokenPair>>;

public class RegisterUserCommandHandler(IMongoDatabase database, IJwtService jwtService, IPasswordHasher passwordHasher, ILogger<RegisterUserCommandHandler> logger) : IRequestHandler<RegisterUserCommand, ErrorOr<TokenPair>>
{
    private readonly IMongoCollection<User> _collection = database.GetCollection<User>("Users");

    public async ValueTask<ErrorOr<TokenPair>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var dto = request.RegistrationDto;
        logger.LogInformation("Starting registration process for Nickname: {Nickname}", dto.Nickname);

        var existingUser = await _collection.Find(u => u.Nickname == dto.Nickname).FirstOrDefaultAsync(cancellationToken);
        if (existingUser is not null)
        {
            logger.LogWarning("Registration failed. Nickname {Nickname} is already taken", dto.Nickname);
            return UserErrors.NicknameConflict(dto.Nickname);
        }

        var newUser = new User
        {
            Nickname = dto.Nickname,
            FullName = dto.FullName,
            Role = GuestRole.Instance,
            PasswordHash = passwordHasher.HashPassword(dto.Password),
            PasswordRecreationRequired = false,
            PasswordLastChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _collection.InsertOneAsync(newUser, cancellationToken: cancellationToken);
        logger.LogInformation("User {Nickname} registered and saved with ID: {UserId}", newUser.Nickname, newUser.Id);

        return await jwtService.GenerateTokenPairAsync(newUser.Id, newUser.Nickname, newUser.Role);
    }
}
