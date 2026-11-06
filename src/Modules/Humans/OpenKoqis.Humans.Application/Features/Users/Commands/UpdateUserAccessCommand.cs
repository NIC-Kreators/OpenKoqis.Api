using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Roles;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;

#pragma warning disable CS9113 // Handle is not implemented yet, so the injected dependencies are unread.

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
    public ValueTask<ErrorOr<UpdateUserAccessCommandResponse>> Handle(UpdateUserAccessCommandRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
