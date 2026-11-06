using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Roles;
using OpenKoqis.Humans.Application.Features.Roles.Errors;
using OpenKoqis.Humans.Application.Features.Users.Errors;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Humans.Application.Services.Errors;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Humans.Domain.Users.Errors;
using OpenKoqis.Humans.Domain.Users.Types;
using OpenKoqis.Shared.Kernel.Access;

namespace OpenKoqis.Humans.Application.Features.Users.Commands;

public class CreateUserCommandRequest : ICommand<ErrorOr<CreateUserCommandResponse>>
{
    public required string Login { get; init; }
    public required string FullName { get; init; }

    /// <summary>
    /// Password of the identity to create. Required and non-blank when no identity with <see cref="Login"/> exists;
    /// must be <c>null</c> when one does — any string, a blank one included, is rejected then.
    /// </summary>
    public string? Password { get; init; }

    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public Guid? RoleId { get; init; }

    /// <summary>
    /// Accesses granted to the user directly, each written as <c>resource:permission,permission</c>.
    /// </summary>
    public required IReadOnlyList<string> DedicatedAccess { get; init; }
}

public record CreateUserCommandResponse
{
    public required UserDto User { get; init; }
}

/// <summary>
/// Creates a regular user linked to the identity with the given login. When no such identity exists and
/// <see cref="HumansOptions.IsIdentityCreationAllowed"/> is set, the identity is created first; if the user is then
/// not saved, that identity is deleted again unless a persisted user is linked to it. After the user is saved, its
/// access is pushed through <see cref="IAccessProjection"/>.
/// </summary>
public class CreateUserCommandHandler(
    IHumansUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IIdentityProvider identityProvider,
    IAccessProjection accessProjection,
    IOptionsMonitor<HumansOptions> options,
    ILogger<CreateUserCommandHandler> logger) : ICommandHandler<CreateUserCommandRequest, ErrorOr<CreateUserCommandResponse>>
{
    public async ValueTask<ErrorOr<CreateUserCommandResponse>> Handle(CreateUserCommandRequest request, CancellationToken cancellationToken)
    {
        var parsedLogin = Login.Parse(request.Login);
        var parsedName = HumanName.Parse(request.FullName);
        var parsedEmail = request.Email is null ? null : (ErrorOr<Email>?)Email.Parse(request.Email);
        var parsedPhoneNumber = request.PhoneNumber is null ? null : (ErrorOr<PhoneNumber>?)PhoneNumber.Parse(request.PhoneNumber);
        var parsedAccesses = request.DedicatedAccess.Select(Access.Parse).ToList();

        List<Error> inputErrors =
        [
            .. parsedLogin.ErrorsOrEmptyList,
            .. parsedName.ErrorsOrEmptyList,
            .. parsedEmail?.ErrorsOrEmptyList ?? [],
            .. parsedPhoneNumber?.ErrorsOrEmptyList ?? [],
            .. parsedAccesses.SelectMany(a => a.ErrorsOrEmptyList),
        ];

        if (request.DedicatedAccess.Count is 0)
            inputErrors.Add(UserErrors.AtLeastOneAccessShouldBeDefined);

        if (request.RoleId == Guid.Empty)
            inputErrors.Add(UserErrors.EmptyRoleId);

        if (inputErrors.Count is not 0)
            return inputErrors;

        var login = parsedLogin.Value;

        Role? role = null;

        if (request.RoleId is { } roleId)
        {
            role = await roleRepository.GetRoleWithId(roleId, cancellationToken);

            if (role is null)
                return RoleFeatureErrors.NotFound(roleId);
        }

        Identity? createdIdentity = null;
        var isCommitted = false;

        try
        {
            var identity = await identityProvider.FindByLogin(login, cancellationToken);

            if (identity is null)
            {
                if (!options.CurrentValue.IsIdentityCreationAllowed)
                    return IdentityErrors.NotFound(login);

                if (string.IsNullOrWhiteSpace(request.Password))
                    return IdentityErrors.EmptyPassword;

                var created = await identityProvider.Create(login, request.Password, cancellationToken);

                if (created.IsError)
                    return created.Errors;

                identity = createdIdentity = created.Value;
            }
            else
            {
                if (await userRepository.IsIdentityLinked(identity.Id, cancellationToken))
                    return UserErrors.IdentityAlreadyLinked(identity.Id);

                if (request.Password is not null)
                    return UserFeatureErrors.PasswordNotAllowedForExistingIdentity;
            }

            var user = User.Create(new User.CreationAttributes
            {
                Login = login,
                IdentityId = identity.Id,
                Name = parsedName.Value,
                RoleId = request.RoleId,
                DedicatedAccess = parsedAccesses.Select(a => a.Value),
                Email = parsedEmail?.Value,
                PhoneNumber = parsedPhoneNumber?.Value,
            });

            if (user.IsError)
                return user.Errors;

            userRepository.Add(user.Value);

            var saved = await unitOfWork.SaveChangesAsync(cancellationToken);

            if (saved.IsError)
                return saved.Errors;

            isCommitted = true;

            if (!await TryProjectAccessAsync(user.Value, role, cancellationToken))
                return UserFeatureErrors.AccessProjectionFailed(user.Value.Id);

            return new CreateUserCommandResponse { User = UserDto.FromUser(user.Value) };
        }
        finally
        {
            if (!isCommitted && createdIdentity is not null)
                await IdentityCompensation.DeleteUnlessLinkedAsync(
                    identityProvider, userRepository, createdIdentity, "user creation", logger);
        }
    }

    private async Task<bool> TryProjectAccessAsync(User user, Role? role, CancellationToken cancellationToken)
    {
        try
        {
            var projected = await accessProjection.UserAccessChanged(user, role, cancellationToken);

            if (!projected.IsError)
                return true;

            logger.LogError(
                "User {UserId} was saved, but projecting its access failed. Errors: {Errors}",
                user.Id,
                projected.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "User {UserId} was saved, but projecting its access threw", user.Id);
        }

        return false;
    }
}
