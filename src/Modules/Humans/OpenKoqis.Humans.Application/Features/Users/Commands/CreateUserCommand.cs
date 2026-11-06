using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Roles;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;

#pragma warning disable CS9113 // Handle is not implemented yet, so the injected dependencies are unread.

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
    public ValueTask<ErrorOr<CreateUserCommandResponse>> Handle(CreateUserCommandRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
