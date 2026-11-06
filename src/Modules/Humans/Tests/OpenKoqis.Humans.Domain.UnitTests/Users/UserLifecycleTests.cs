using AwesomeAssertions;
using ErrorOr;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Humans.Domain.Users.Types;
using static OpenKoqis.Humans.Domain.UnitTests.Fixtures;

namespace OpenKoqis.Humans.Domain.UnitTests.Users;

public class UserLifecycleTests
{
    private static User CreateUser(Login? login = null, Email? email = null, PhoneNumber? phoneNumber = null)
        => User.Create(new User.CreationAttributes
        {
            Login = login ?? LoginOf("ivan"),
            IdentityId = "identity-1",
            Name = HumanNameOf("Ivan Ivanov"),
            DedicatedAccess = AccessesOf("bins:read"),
            Email = email,
            PhoneNumber = phoneNumber,
        }).Value;

    private static User CreateRoot()
        => User.CreateRoot(LoginOf("root@example.com"), "identity-root", HumanNameOf("Root")).Value;

    private static Email EmailOf(string raw) => Email.Parse(raw).Value;

    private static PhoneNumber PhoneOf(string raw) => PhoneNumber.Parse(raw).Value;

    /// <summary>
    /// UpdateEmail succeeds and returns <see cref="Result.Updated"/>.
    /// </summary>
    [Test]
    public void UpdateEmail_ValidEmail_ReturnsUpdated()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var result = user.UpdateEmail(EmailOf("ivan@example.com"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Updated);
    }

    /// <summary>
    /// UpdateEmail sets the contact email exactly as given.
    /// </summary>
    [Test]
    public void UpdateEmail_NoExistingEmail_SetsIt()
    {
        // Arrange
        var user = CreateUser();
        var value = EmailOf("ivan@example.com");

        // Act
        user.UpdateEmail(value);

        // Assert
        user.Email.Should().Be(value);
    }

    /// <summary>
    /// UpdateEmail replaces an existing contact email.
    /// </summary>
    [Test]
    public void UpdateEmail_ExistingEmail_ReplacesIt()
    {
        // Arrange
        var user = CreateUser(email: EmailOf("old@example.com"), phoneNumber: PhoneOf("+77001111111"));
        var value = EmailOf("new@example.com");

        // Act
        user.UpdateEmail(value);

        // Assert
        user.Email.Should().Be(value);
    }

    /// <summary>
    /// UpdateEmail with <c>null</c> clears the contact email.
    /// </summary>
    [Test]
    public void UpdateEmail_Null_ClearsIt()
    {
        // Arrange
        var user = CreateUser(email: EmailOf("old@example.com"), phoneNumber: PhoneOf("+77001111111"));

        // Act
        user.UpdateEmail(null);

        // Assert
        user.Email.Should().BeNull();
    }

    /// <summary>
    /// UpdateEmail leaves the contact phone number untouched, whether the email is set or cleared.
    /// </summary>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public void UpdateEmail_Any_KeepsPhoneNumber(bool clear)
    {
        // Arrange
        var user = CreateUser(email: EmailOf("old@example.com"), phoneNumber: PhoneOf("+77001111111"));
        var other = user.PhoneNumber;

        // Act
        user.UpdateEmail(clear ? null : EmailOf("new@example.com"));

        // Assert
        other.Should().NotBeNull();
        user.PhoneNumber.Should().Be(other);
    }

    /// <summary>
    /// UpdateEmail does not change the login.
    /// </summary>
    [Test]
    public void UpdateEmail_Any_KeepsLogin()
    {
        // Arrange
        var login = LoginOf("ivan");
        var user = CreateUser(login);

        // Act
        user.UpdateEmail(EmailOf("new@example.com"));

        // Assert
        user.Login.Should().Be(login);
    }

    /// <summary>
    /// For a user whose login is an email, <c>null</c> resets the contact email to the login instead of clearing it.
    /// </summary>
    [Test]
    public void UpdateEmail_EmailLoginAndNull_ResetsEmailToLogin()
    {
        // Arrange
        var login = EmailOf("ivan@example.com");
        var user = CreateUser(login, email: EmailOf("other@example.com"));

        // Act
        user.UpdateEmail(null);

        // Assert
        user.Email.Should().Be(login);
        user.Login.Should().Be(login);
    }

    /// <summary>
    /// UpdateEmail moves UpdatedAt to the time of the call or later.
    /// </summary>
    [Test]
    public void UpdateEmail_Any_UpdatesUpdatedAt()
    {
        // Arrange
        var user = CreateUser();
        var before = DateTime.UtcNow;

        // Act
        user.UpdateEmail(EmailOf("ivan@example.com"));

        // Assert
        user.UpdatedAt.Should().BeOnOrAfter(before);
    }

    /// <summary>
    /// UpdateEmail is allowed for the root user.
    /// </summary>
    [Test]
    public void UpdateEmail_Root_Succeeds()
    {
        // Arrange
        var root = CreateRoot();
        var value = EmailOf("new@example.com");

        // Act
        var result = root.UpdateEmail(value);

        // Assert
        result.IsError.Should().BeFalse();
        root.Email.Should().Be(value);
    }

    /// <summary>
    /// UpdatePhoneNumber succeeds and returns <see cref="Result.Updated"/>.
    /// </summary>
    [Test]
    public void UpdatePhoneNumber_ValidPhoneNumber_ReturnsUpdated()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var result = user.UpdatePhoneNumber(PhoneOf("+77001234567"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Updated);
    }

    /// <summary>
    /// UpdatePhoneNumber sets the contact phone number exactly as given.
    /// </summary>
    [Test]
    public void UpdatePhoneNumber_NoExistingPhoneNumber_SetsIt()
    {
        // Arrange
        var user = CreateUser();
        var value = PhoneOf("+77001234567");

        // Act
        user.UpdatePhoneNumber(value);

        // Assert
        user.PhoneNumber.Should().Be(value);
    }

    /// <summary>
    /// UpdatePhoneNumber replaces an existing contact phone number.
    /// </summary>
    [Test]
    public void UpdatePhoneNumber_ExistingPhoneNumber_ReplacesIt()
    {
        // Arrange
        var user = CreateUser(email: EmailOf("old@example.com"), phoneNumber: PhoneOf("+77001111111"));
        var value = PhoneOf("+77002222222");

        // Act
        user.UpdatePhoneNumber(value);

        // Assert
        user.PhoneNumber.Should().Be(value);
    }

    /// <summary>
    /// UpdatePhoneNumber with <c>null</c> clears the contact phone number.
    /// </summary>
    [Test]
    public void UpdatePhoneNumber_Null_ClearsIt()
    {
        // Arrange
        var user = CreateUser(email: EmailOf("old@example.com"), phoneNumber: PhoneOf("+77001111111"));

        // Act
        user.UpdatePhoneNumber(null);

        // Assert
        user.PhoneNumber.Should().BeNull();
    }

    /// <summary>
    /// UpdatePhoneNumber leaves the contact email untouched, whether the phone number is set or cleared.
    /// </summary>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public void UpdatePhoneNumber_Any_KeepsEmail(bool clear)
    {
        // Arrange
        var user = CreateUser(email: EmailOf("old@example.com"), phoneNumber: PhoneOf("+77001111111"));
        var other = user.Email;

        // Act
        user.UpdatePhoneNumber(clear ? null : PhoneOf("+77002222222"));

        // Assert
        other.Should().NotBeNull();
        user.Email.Should().Be(other);
    }

    /// <summary>
    /// UpdatePhoneNumber does not change the login.
    /// </summary>
    [Test]
    public void UpdatePhoneNumber_Any_KeepsLogin()
    {
        // Arrange
        var login = LoginOf("ivan");
        var user = CreateUser(login);

        // Act
        user.UpdatePhoneNumber(PhoneOf("+77002222222"));

        // Assert
        user.Login.Should().Be(login);
    }

    /// <summary>
    /// For a user whose login is a phone number, <c>null</c> resets the contact phone number to the login instead of
    /// clearing it.
    /// </summary>
    [Test]
    public void UpdatePhoneNumber_PhoneNumberLoginAndNull_ResetsPhoneNumberToLogin()
    {
        // Arrange
        var login = PhoneOf("+77001234567");
        var user = CreateUser(login, phoneNumber: PhoneOf("+77002222222"));

        // Act
        user.UpdatePhoneNumber(null);

        // Assert
        user.PhoneNumber.Should().Be(login);
        user.Login.Should().Be(login);
    }

    /// <summary>
    /// UpdatePhoneNumber moves UpdatedAt to the time of the call or later.
    /// </summary>
    [Test]
    public void UpdatePhoneNumber_Any_UpdatesUpdatedAt()
    {
        // Arrange
        var user = CreateUser();
        var before = DateTime.UtcNow;

        // Act
        user.UpdatePhoneNumber(PhoneOf("+77001234567"));

        // Assert
        user.UpdatedAt.Should().BeOnOrAfter(before);
    }

    /// <summary>
    /// UpdatePhoneNumber is allowed for the root user.
    /// </summary>
    [Test]
    public void UpdatePhoneNumber_Root_Succeeds()
    {
        // Arrange
        var root = CreateRoot();
        var value = PhoneOf("+77002222222");

        // Act
        var result = root.UpdatePhoneNumber(value);

        // Assert
        result.IsError.Should().BeFalse();
        root.PhoneNumber.Should().Be(value);
    }

    /// <summary>
    /// Delete succeeds and returns <see cref="Result.Deleted"/>.
    /// </summary>
    [Test]
    public void Delete_ActiveUser_ReturnsDeleted()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var result = user.Delete();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Deleted);
    }

    /// <summary>
    /// Delete sets DeletedAt to a UTC time no earlier than the call.
    /// </summary>
    [Test]
    public void Delete_ActiveUser_SetsDeletedAtUtc()
    {
        // Arrange
        var user = CreateUser();
        var before = DateTime.UtcNow;

        // Act
        user.Delete();

        // Assert
        user.DeletedAt.Should().NotBeNull();
        user.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        user.DeletedAt.Value.Should().BeOnOrAfter(before);
    }

    /// <summary>
    /// Deleting the root fails with <c>User.RootCannotBeDeleted</c>.
    /// </summary>
    [Test]
    public void Delete_Root_ReturnsRootCannotBeDeleted()
    {
        // Arrange
        var root = CreateRoot();

        // Act
        var result = root.Delete();

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Equal("User.RootCannotBeDeleted");
    }

    /// <summary>
    /// A rejected deletion of the root leaves DeletedAt null.
    /// </summary>
    [Test]
    public void Delete_Root_KeepsDeletedAtNull()
    {
        // Arrange
        var root = CreateRoot();

        // Act
        root.Delete();

        // Assert
        root.DeletedAt.Should().BeNull();
    }

    /// <summary>
    /// Deleting an already deleted user fails with <c>User.AlreadyDeleted</c>.
    /// </summary>
    [Test]
    public void Delete_AlreadyDeleted_ReturnsAlreadyDeleted()
    {
        // Arrange
        var user = CreateUser();
        user.Delete();

        // Act
        var result = user.Delete();

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Equal("User.AlreadyDeleted");
    }

    /// <summary>
    /// A second Delete keeps the DeletedAt set by the first one.
    /// </summary>
    [Test]
    public async Task Delete_AlreadyDeleted_KeepsFirstDeletedAt()
    {
        // Arrange
        var user = CreateUser();
        user.Delete();
        var firstDeletedAt = user.DeletedAt;
        await Task.Delay(10);

        // Act
        user.Delete();

        // Assert
        firstDeletedAt.Should().NotBeNull();
        user.DeletedAt.Should().Be(firstDeletedAt);
    }
}
