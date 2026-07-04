using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Application.Services; // Assuming IJwtService and IPasswordHasher are here
using OpenKoqis.Domain.Models;
using OpenKoqis.Domain.Models.Dto;

namespace OpenKoqis.Application.Features.Users.Commands;

public record RegisterUserCommand(UserRegistrationDto RegistrationDto) : IRequest<ErrorOr<TokenPair>>;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, ErrorOr<TokenPair>>
{
    private readonly IMongoCollection<User> _collection;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IMongoDatabase database,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _collection = database.GetCollection<User>("Users");
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<ErrorOr<TokenPair>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var dto = request.RegistrationDto;
        _logger.LogInformation("Starting registration process for Nickname: {Nickname}", dto.Nickname);

        var existingUser = await _collection.Find(u => u.Nickname == dto.Nickname).FirstOrDefaultAsync(cancellationToken);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed. Nickname {Nickname} is already taken", dto.Nickname);
            return UserErrors.NicknameConflict(dto.Nickname);
        }

        string hashedPassword = _passwordHasher.HashPassword(dto.Password);

        var newUser = new User
        {
            Nickname = dto.Nickname,
            FullName = dto.FullName,
            Role = GuestRole.Instance,
            PasswordHash = hashedPassword,
            PasswordRecreationRequired = false,
            PasswordLastChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _collection.InsertOneAsync(newUser, cancellationToken: cancellationToken);
        _logger.LogInformation("User {Nickname} registered and saved with ID: {UserId}", newUser.Nickname, newUser.Id);

        var tokenPair = await _jwtService.GenerateTokenPairAsync(newUser.Id, newUser.Nickname, newUser.Role);
        return tokenPair;
    }
}
