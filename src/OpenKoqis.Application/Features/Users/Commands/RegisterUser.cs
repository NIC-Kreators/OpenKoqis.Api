using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenKoqis.Application.Features.Users.Errors;
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

        var usernameOrError = Username.Create(dto.Nickname);
        if (usernameOrError.IsError)
            return usernameOrError.Errors;

        var fullNameOrError = FullName.Create(dto.FullName);
        if (fullNameOrError.IsError)
            return fullNameOrError.Errors;

        if (await _collection.Find(u => u.Nickname.Value == dto.Nickname).AnyAsync(cancellationToken))
            return UserErrors.NicknameConflict(dto.Nickname);

        var user = new User(
            ObjectId.GenerateNewId().ToString(),
            usernameOrError.Value,
            fullNameOrError.Value,
            new PasswordHash(passwordHasher.HashPassword(dto.Password)),
            GuestRole.Instance);

        await _collection.InsertOneAsync(user, null, cancellationToken);
        logger.LogInformation("User {Nickname} registered with ID: {UserId}", user.Nickname.Value, user.Id);

        return await jwtService.GenerateTokenPairAsync(user.Id, user.Nickname.Value, user.Role);
    }
}
