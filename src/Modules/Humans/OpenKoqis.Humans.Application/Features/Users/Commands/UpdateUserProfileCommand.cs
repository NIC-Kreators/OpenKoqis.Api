using ErrorOr;
using Mediator;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Users.Errors;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Domain.Users.Types;

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
    IUserRepository userRepository) : ICommandHandler<UpdateUserProfileCommandRequest, ErrorOr<UpdateUserProfileCommandResponse>>
{
    public async ValueTask<ErrorOr<UpdateUserProfileCommandResponse>> Handle(UpdateUserProfileCommandRequest request, CancellationToken cancellationToken)
    {
        var parsedName = HumanName.Parse(request.FullName);
        var parsedEmail = request.Email is null ? (ErrorOr<Email>?)null : Email.Parse(request.Email);
        var parsedPhoneNumber = request.PhoneNumber is null ? (ErrorOr<PhoneNumber>?)null : PhoneNumber.Parse(request.PhoneNumber);

        List<Error> inputErrors =
        [
            .. parsedName.ErrorsOrEmptyList,
            .. parsedEmail?.ErrorsOrEmptyList ?? [],
            .. parsedPhoneNumber?.ErrorsOrEmptyList ?? [],
        ];

        if (inputErrors.Count is not 0)
            return inputErrors;

        var user = await userRepository.GetUserWithId(request.Id, cancellationToken);

        if (user is null)
            return UserFeatureErrors.NotFound(request.Id);

        var renamed = user.Rename(parsedName.Value);

        if (renamed.IsError)
            return renamed.Errors;

        var emailUpdated = user.UpdateEmail(parsedEmail?.Value);

        if (emailUpdated.IsError)
            return emailUpdated.Errors;

        var phoneNumberUpdated = user.UpdatePhoneNumber(parsedPhoneNumber?.Value);

        if (phoneNumberUpdated.IsError)
            return phoneNumberUpdated.Errors;

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);

        if (saved.IsError)
            return saved.Errors;

        return new UpdateUserProfileCommandResponse { User = UserDto.FromUser(user) };
    }
}
