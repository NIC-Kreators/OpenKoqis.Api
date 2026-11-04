using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Users.Errors;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Humans.Application.Services.Errors;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Humans.Domain.Users.Errors;
using OpenKoqis.Humans.Domain.Users.Types;
using OpenKoqis.Shared.Application.Services;

namespace OpenKoqis.Humans.Application.Features.Users.Commands;

public class CreateRootAdminCommandRequest : ICommand<ErrorOr<CreateRootAdminCommandResponse>>
{
    public required string Login { get; init; }
    public required string FullName { get; init; }
    public required string Password { get; init; }
}

public record CreateRootAdminCommandResponse
{
    public required UserDto User { get; init; }
    public required TokensDto Tokens { get; init; }
}

/// <summary>
/// Creates the root administrator and signs them in. The user is linked to the identity with the given login,
/// or, when <see cref="HumansOptions.IsIdentityCreationAllowed"/> is set, to a newly created one.
/// An identity created by this call is deleted again if anything after its creation fails.
/// </summary>
public class CreateRootAdminCommandHandler(
    IHumansUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IIdentityProvider identityProvider,
    ILockManager lockManager,
    IOptionsMonitor<HumansOptions> options,
    ILogger<CreateRootAdminCommandHandler> logger) : ICommandHandler<CreateRootAdminCommandRequest, ErrorOr<CreateRootAdminCommandResponse>>
{
    private const string LockKey = "RootAdminCreation";

    public async ValueTask<ErrorOr<CreateRootAdminCommandResponse>> Handle(CreateRootAdminCommandRequest request, CancellationToken cancellationToken)
    {
        var parsedLogin = Login.Parse(request.Login);
        var parsedName = HumanName.Parse(request.FullName);

        List<Error> inputErrors = [.. parsedLogin.ErrorsOrEmptyList, .. parsedName.ErrorsOrEmptyList];

        if (string.IsNullOrWhiteSpace(request.Password))
            inputErrors.Add(IdentityErrors.EmptyPassword);

        if (inputErrors.Count is not 0)
            return inputErrors;

        var login = parsedLogin.Value;

        await using var lease = await lockManager.TryLeaseLockAsync(LockKey, logger: logger, ct: cancellationToken);

        if (lease is null)
            return RootAdminErrors.CreationInProgress;

        if (await userRepository.IsRootAlreadyExists(cancellationToken))
            return UserErrors.RootAlreadyExists;

        var isCreationAllowed = options.CurrentValue.IsIdentityCreationAllowed;
        var identity = await identityProvider.FindByLogin(login, cancellationToken);
        var isIdentityCreatedHere = false;

        if (identity is null)
        {
            if (!isCreationAllowed)
                return IdentityErrors.NotFound(login);

            var created = await identityProvider.Create(login, request.Password, cancellationToken);

            if (created.IsError)
                return created.Errors;

            identity = created.Value;
            isIdentityCreatedHere = true;
        }

        // Also proves that the caller owns an identity that already existed.
        var tokens = await identityProvider.RequestTokens(login, request.Password, cancellationToken);

        if (tokens.IsError)
        {
            if (isIdentityCreatedHere)
                await RollbackCreatedIdentityAsync(identity);

            return tokens.Errors;
        }

        var user = User.CreateRoot(login, identity.Id, parsedName.Value);

        if (user.IsError)
        {
            if (isIdentityCreatedHere)
                await RollbackCreatedIdentityAsync(identity);

            await identityProvider.RevokeTokens(tokens.Value.RefreshToken, cancellationToken);

            return user.Errors;
        }

        userRepository.Add(user.Value);

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);

        if (saved.IsError)
        {
            if (isIdentityCreatedHere)
                await RollbackCreatedIdentityAsync(identity);

            await identityProvider.RevokeTokens(tokens.Value.RefreshToken, cancellationToken);

            return saved.Errors;
        }

        return new CreateRootAdminCommandResponse
        {
            User = UserDto.FromUser(user.Value),
            Tokens = tokens.Value,
        };
    }

    private async Task RollbackCreatedIdentityAsync(Identity identity)
    {
        // CancellationToken.None: the cleanup must run even when the request itself was cancelled.
        var deleted = await identityProvider.Delete(identity.Id, CancellationToken.None);

        if (deleted.IsError)
            logger.LogError(
                "Failed to delete identity {IdentityId} (login {Login}) after root admin creation failed; it is orphaned in the identity provider. Errors: {Errors}",
                identity.Id,
                identity.Login.Value,
                deleted.Errors);
    }
}
