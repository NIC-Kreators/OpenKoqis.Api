using AwesomeAssertions;
using ErrorOr;
using NSubstitute;
using OpenKoqis.Humans.Application.Features.Users;
using OpenKoqis.Humans.Application.Features.Users.Commands;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Humans.Domain.Users.Types;
using static OpenKoqis.Humans.Application.UnitTests.Fixtures;

namespace OpenKoqis.Humans.Application.UnitTests.Users;

public class UpdateUserProfileCommandTests
{
    private readonly IHumansUnitOfWork _unitOfWork = Substitute.For<IHumansUnitOfWork>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private UpdateUserProfileCommandHandler CreateHandler()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success.ToErrorOr());

        return new UpdateUserProfileCommandHandler(
            _unitOfWork,
            _userRepository);
    }

    private static UpdateUserProfileCommandRequest RequestFor(
        Guid id,
        string fullName = "Petr Petrov",
        string? email = "petr@example.com",
        string? phoneNumber = "+77011234567")
        => new() { Id = id, FullName = fullName, Email = email, PhoneNumber = phoneNumber };

    private static User UserWithContacts(string login = "ivan", string email = "old@example.com", string phone = "+77019999999")
        => User.Create(new User.CreationAttributes
        {
            Login = LoginOf(login),
            IdentityId = "identity-1",
            Name = HumanNameOf(),
            DedicatedAccess = AccessesOf("bins:read"),
            Email = Email.Parse(email).Value,
            PhoneNumber = PhoneNumber.Parse(phone).Value,
        }).Value;

    private User GivenExisting(User user)
    {
        _userRepository.GetUserWithId(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    /// <summary>
    /// A valid request replaces the name and both contacts on the tracked user and saves once.
    /// </summary>
    [Test]
    public async Task Handle_ValidRequest_ReplacesNameAndContactsAndSavesOnce()
    {
        // Arrange
        var user = GivenExisting(UserWithContacts());
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(RequestFor(user.Id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        user.Name.FullName.Should().Be("Petr Petrov");
        user.Email!.Value.Should().Be("petr@example.com");
        user.PhoneNumber!.Value.Should().Be("+77011234567");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// On success the response carries the user with the new name and contacts.
    /// </summary>
    [Test]
    public async Task Handle_ValidRequest_ReturnsUserWithNewNameAndContacts()
    {
        // Arrange
        var user = GivenExisting(UserWithContacts());
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(RequestFor(user.Id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.User.Id.Should().Be(user.Id);
        result.Value.User.FullName.Should().Be("Petr Petrov");
        result.Value.User.Email.Should().Be("petr@example.com");
        result.Value.User.PhoneNumber.Should().Be("+77011234567");
    }

    /// <summary>
    /// The login of the user is not changed by a profile update.
    /// </summary>
    [Test]
    public async Task Handle_ValidRequest_KeepsLoginUnchanged()
    {
        // Arrange
        var user = GivenExisting(UserWithContacts(login: "ivan"));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(RequestFor(user.Id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.User.Login.Should().Be("ivan");
        user.Login.Value.Should().Be("ivan");
    }

    /// <summary>
    /// A null email clears the existing email and leaves the phone number set from the request.
    /// </summary>
    [Test]
    public async Task Handle_NullEmail_ClearsExistingEmail()
    {
        // Arrange
        var user = GivenExisting(UserWithContacts());
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(RequestFor(user.Id, email: null), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        user.Email.Should().BeNull();
        user.PhoneNumber!.Value.Should().Be("+77011234567");
        result.Value.User.Email.Should().BeNull();
    }

    /// <summary>
    /// A null phone number clears the existing phone number and leaves the email set from the request.
    /// </summary>
    [Test]
    public async Task Handle_NullPhoneNumber_ClearsExistingPhoneNumber()
    {
        // Arrange
        var user = GivenExisting(UserWithContacts());
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(RequestFor(user.Id, phoneNumber: null), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        user.PhoneNumber.Should().BeNull();
        user.Email!.Value.Should().Be("petr@example.com");
        result.Value.User.PhoneNumber.Should().BeNull();
    }

    /// <summary>
    /// For a user whose login is an email, a null email resets the contact email to the login.
    /// </summary>
    [Test]
    public async Task Handle_NullEmailForEmailLoginUser_ResetsEmailToLogin()
    {
        // Arrange
        var user = GivenExisting(UserWithContacts(login: "ivan@example.com"));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(RequestFor(user.Id, email: null), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        user.Email.Should().Be(user.Login);
        result.Value.User.Email.Should().Be("ivan@example.com");
        result.Value.User.Login.Should().Be("ivan@example.com");
    }

    /// <summary>
    /// An invalid full name is rejected with the name error code.
    /// </summary>
    [Test]
    [Arguments("", "HumanName.WrongAmountOfWords")]
    [Arguments("Ivan Ivanovich Ivanov Extra", "HumanName.WrongAmountOfWords")]
    [Arguments("Iv4n", "HumanName.AllSymbolsShouldBeALetter")]
    public async Task Handle_InvalidFullName_ReturnsNameError(string fullName, string expectedCode)
    {
        // Arrange
        var user = GivenExisting(UserWithContacts());
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(RequestFor(user.Id, fullName: fullName), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain(expectedCode);
    }

    /// <summary>
    /// An invalid email is rejected with the invalid email code.
    /// </summary>
    [Test]
    public async Task Handle_InvalidEmail_ReturnsInvalidEmailError()
    {
        // Arrange
        var user = GivenExisting(UserWithContacts());
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(RequestFor(user.Id, email: "not-an-email"), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("Login.InvalidEmail");
    }

    /// <summary>
    /// An invalid phone number is rejected with the invalid phone number code.
    /// </summary>
    [Test]
    public async Task Handle_InvalidPhoneNumber_ReturnsInvalidPhoneNumberError()
    {
        // Arrange
        var user = GivenExisting(UserWithContacts());
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(RequestFor(user.Id, phoneNumber: "12"), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("Login.InvalidPhoneNumber");
    }

    /// <summary>
    /// An invalid name, email and phone number together are all reported in one result.
    /// </summary>
    [Test]
    public async Task Handle_AllFieldsInvalid_ReturnsAllErrorsTogether()
    {
        // Arrange
        var user = GivenExisting(UserWithContacts());
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            RequestFor(user.Id, fullName: "Iv4n", email: "not-an-email", phoneNumber: "12"),
            CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain(
            ["HumanName.AllSymbolsShouldBeALetter", "Login.InvalidEmail", "Login.InvalidPhoneNumber"]);
    }

    /// <summary>
    /// When input validation fails, no repository or unit of work call is made.
    /// </summary>
    [Test]
    public async Task Handle_InvalidInput_CallsNoPort()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            RequestFor(Guid.NewGuid(), fullName: "Iv4n", email: "not-an-email", phoneNumber: "12"),
            CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        await _userRepository.DidNotReceiveWithAnyArgs().GetUserWithId(Guid.Empty);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync();
    }

    /// <summary>
    /// An unknown user id yields User.NotFound and nothing is saved.
    /// </summary>
    [Test]
    public async Task Handle_UserNotFound_ReturnsNotFoundAndDoesNotSave()
    {
        // Arrange
        var id = Guid.NewGuid();
        _userRepository.GetUserWithId(id, Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(RequestFor(id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("User.NotFound");
        result.Errors.Should().Contain(e => e.Type == ErrorType.NotFound);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync();
    }

    /// <summary>
    /// Errors returned by the save are returned to the caller.
    /// </summary>
    [Test]
    public async Task Handle_SaveReturnsError_ReturnsThatError()
    {
        // Arrange
        var user = GivenExisting(UserWithContacts());
        var handler = CreateHandler();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns((ErrorOr<Success>)Error.Failure("Db.Failed"));

        // Act
        var result = await handler.Handle(RequestFor(user.Id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("Db.Failed");
    }

    /// <summary>
    /// The root user can have its name and contacts edited like any other user.
    /// </summary>
    [Test]
    public async Task Handle_RootUser_IsEditedAndSaved()
    {
        // Arrange
        var root = GivenExisting(RootUser());
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(RequestFor(root.Id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        root.Name.FullName.Should().Be("Petr Petrov");
        root.Email!.Value.Should().Be("petr@example.com");
        root.PhoneNumber!.Value.Should().Be("+77011234567");
        result.Value.User.FullName.Should().Be("Petr Petrov");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
