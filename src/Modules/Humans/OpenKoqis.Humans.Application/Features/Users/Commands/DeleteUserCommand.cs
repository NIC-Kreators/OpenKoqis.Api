using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;

#pragma warning disable CS9113 // Handle is not implemented yet, so the injected dependencies are unread.

namespace OpenKoqis.Humans.Application.Features.Users.Commands;

public class DeleteUserCommandRequest : ICommand<ErrorOr<Deleted>>
{
    public required Guid Id { get; init; }
}

/// <summary>
/// Soft-deletes a user and disables its identity, which also ends its sessions. If the user is not saved, an
/// identity disabled by this call is enabled again. An identity that no longer exists in the provider does not
/// block the deletion.
/// </summary>
public class DeleteUserCommandHandler(
    IHumansUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IIdentityProvider identityProvider,
    ILogger<DeleteUserCommandHandler> logger) : ICommandHandler<DeleteUserCommandRequest, ErrorOr<Deleted>>
{
    public ValueTask<ErrorOr<Deleted>> Handle(DeleteUserCommandRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
