using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Persistence;

#pragma warning disable CS9113 // Handle is not implemented yet, so the injected dependencies are unread.

namespace OpenKoqis.Humans.Application.Features.Users.Commands;

/// <summary>
/// The full new profile of the user. A <c>null</c> contact falls back to its default: the login when the login is of
/// that kind, otherwise no contact.
/// </summary>
public class UpdateUserProfileCommandRequest : ICommand<ErrorOr<UpdateUserProfileCommandResponse>>
{
    public required Guid Id { get; init; }
    public required string FullName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
}

public record UpdateUserProfileCommandResponse
{
    public required UserDto User { get; init; }
}

/// <summary>
/// Replaces the name and contacts of a user. The identity provider is not involved; the login cannot be changed.
/// </summary>
public class UpdateUserProfileCommandHandler(
    IHumansUnitOfWork unitOfWork,
    IUserRepository userRepository,
    ILogger<UpdateUserProfileCommandHandler> logger) : ICommandHandler<UpdateUserProfileCommandRequest, ErrorOr<UpdateUserProfileCommandResponse>>
{
    public ValueTask<ErrorOr<UpdateUserProfileCommandResponse>> Handle(UpdateUserProfileCommandRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
