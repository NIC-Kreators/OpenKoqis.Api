using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Roles;
using OpenKoqis.Humans.Application.Features.Roles.Errors;
using OpenKoqis.Humans.Application.Features.Users.Errors;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Humans.Domain.Users.Errors;
using OpenKoqis.Shared.Kernel.Access;

namespace OpenKoqis.Humans.Application.Features.Users.Commands;

/// <summary>
/// The full new access of the user: the role (<c>null</c> for none) and the dedicated access.
/// </summary>
public class UpdateUserAccessCommandRequest : ICommand<ErrorOr<UpdateUserAccessCommandResponse>>
{
    public required Guid Id { get; init; }
    public Guid? RoleId { get; init; }

    /// <summary>
    /// Accesses granted to the user directly, each written as <c>resource:permission,permission</c>.
    /// </summary>
    public required IReadOnlyList<string> DedicatedAccess { get; init; }
}

public record UpdateUserAccessCommandResponse
{
    public required UserDto User { get; init; }
}

/// <summary>
/// Replaces the role and dedicated access of a user, saves them, then pushes them through
/// <see cref="IAccessProjection"/>. Idempotent: repeating a request re-projects the same access, which is how a
/// failed projection is recovered.
/// </summary>
public class UpdateUserAccessCommandHandler(
    IHumansUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IAccessProjection accessProjection,
    ILogger<UpdateUserAccessCommandHandler> logger) : ICommandHandler<UpdateUserAccessCommandRequest, ErrorOr<UpdateUserAccessCommandResponse>>
{
    public async ValueTask<ErrorOr<UpdateUserAccessCommandResponse>> Handle(UpdateUserAccessCommandRequest request, CancellationToken cancellationToken)
    {
        List<Error> inputErrors = [];
        List<Access> accesses = [];

        foreach (var raw in request.DedicatedAccess)
        {
            var parsed = Access.Parse(raw);

            if (parsed.IsError)
                inputErrors.AddRange(parsed.Errors);
            else
                accesses.Add(parsed.Value);
        }

        if (request.DedicatedAccess.Count is 0)
            inputErrors.Add(UserErrors.AtLeastOneAccessShouldBeDefined);

        if (request.RoleId == Guid.Empty)
            inputErrors.Add(UserErrors.EmptyRoleId);

        if (inputErrors.Count is not 0)
            return inputErrors;

        var user = await userRepository.GetUserWithId(request.Id, cancellationToken);

        if (user is null)
            return UserFeatureErrors.NotFound(request.Id);

        Role? role = null;

        if (request.RoleId is { } roleId)
        {
            role = await roleRepository.GetRoleWithId(roleId, cancellationToken);

            if (role is null)
                return RoleFeatureErrors.NotFound(roleId);
        }

        var roleUpdated = user.UpdateRole(role);

        if (roleUpdated.IsError)
            return roleUpdated.Errors;

        var accessUpdated = user.UpdateDedicatedAccess(accesses, role);

        if (accessUpdated.IsError)
            return accessUpdated.Errors;

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);

        if (saved.IsError)
            return saved.Errors;

        if (!await TryProjectAsync(user, role, cancellationToken))
            return UserFeatureErrors.AccessProjectionFailed(user.Id);

        return new UpdateUserAccessCommandResponse { User = UserDto.FromUser(user) };
    }

    private async Task<bool> TryProjectAsync(User user, Role? role, CancellationToken cancellationToken)
    {
        try
        {
            var projected = await accessProjection.UserAccessChanged(user, role, cancellationToken);

            if (!projected.IsError)
                return true;

            logger.LogError(
                "Failed to project the access of user {UserId}; repeating the request projects it again. Errors: {Errors}",
                user.Id, projected.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project the access of user {UserId}; repeating the request projects it again", user.Id);
        }

        return false;
    }
}
