using AwesomeAssertions;
using ErrorOr;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Humans.Domain.Users.Errors;
using OpenKoqis.Humans.Domain.Users.Types;
using static OpenKoqis.Humans.Domain.UnitTests.Fixtures;

namespace OpenKoqis.Humans.Domain.UnitTests.Users;

public class UserTests
{
    private static User.CreationAttributes ValidAttributes() => new()
    {
        Login = LoginOf("ivan"),
        IdentityId = "identity-1",
        Name = HumanNameOf("Ivan Ivanov"),
        DedicatedAccess = AccessesOf("bins:read"),
    };

    private static User CreateUser(Guid? roleId = null, params string[] dedicatedAccess)
        => User.Create(ValidAttributes() with
        {
            RoleId = roleId,
            DedicatedAccess = AccessesOf(dedicatedAccess.Length > 0 ? dedicatedAccess : ["bins:read"]),
        }).Value;

    private static User CreateRoot()
        => User.CreateRoot(LoginOf("root@example.com"), "identity-root", HumanNameOf("Root")).Value;

    /// <summary>
    /// Valid attributes create a regular (non-root) user with a fresh id, the trimmed identity id and the given
    /// login, name, role and dedicated access; a username login leaves the contact email and phone empty.
    /// </summary>
    [Test]
    public void Create_ValidAttributes_ReturnsRegularUser()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var attributes = ValidAttributes() with { IdentityId = "  identity-1  ", RoleId = roleId };

        // Act
        var result = User.Create(attributes);

        // Assert
        result.IsError.Should().BeFalse();
        var user = result.Value;
        user.Id.Should().NotBeEmpty();
        user.Login.Should().Be(attributes.Login);
        user.IdentityId.Should().Be("identity-1");
        user.Name.Should().Be(attributes.Name);
        user.RoleId.Should().Be(roleId);
        user.DedicatedAccess.Should().BeEquivalentTo(AccessesOf("bins:read"));
        user.IsRoot.Should().BeFalse();
        user.Email.Should().BeNull();
        user.PhoneNumber.Should().BeNull();
        user.LocationId.Should().BeNull();
        user.DeletedAt.Should().BeNull();
        user.UpdatedAt.Should().BeOnOrAfter(user.CreatedAt);
    }

    /// <summary>
    /// A role is optional: a user can be created without one.
    /// </summary>
    [Test]
    public void Create_WithoutRole_Succeeds()
    {
        // Arrange
        var attributes = ValidAttributes() with { RoleId = null };

        // Act
        var result = User.Create(attributes);

        // Assert
        result.Value.RoleId.Should().BeNull();
    }

    /// <summary>
    /// When the login is an <see cref="Email"/> and no contact email is given, the login becomes the contact email.
    /// </summary>
    [Test]
    public void Create_EmailLogin_DefaultsContactEmailToLogin()
    {
        // Arrange
        var login = Email.Parse("ivan@example.com").Value;

        // Act
        var user = User.Create(ValidAttributes() with { Login = login }).Value;

        // Assert
        user.Email.Should().Be(login);
        user.PhoneNumber.Should().BeNull();
    }

    /// <summary>
    /// When the login is a <see cref="PhoneNumber"/> and no contact phone is given, the login becomes the contact phone.
    /// </summary>
    [Test]
    public void Create_PhoneLogin_DefaultsContactPhoneNumberToLogin()
    {
        // Arrange
        var login = PhoneNumber.Parse("+77011234567").Value;

        // Act
        var user = User.Create(ValidAttributes() with { Login = login }).Value;

        // Assert
        user.PhoneNumber.Should().Be(login);
        user.Email.Should().BeNull();
    }

    /// <summary>
    /// An explicitly given contact email wins over an email login.
    /// </summary>
    [Test]
    public void Create_EmailLoginWithExplicitEmail_KeepsExplicitEmail()
    {
        // Arrange
        var contactEmail = Email.Parse("contact@example.com").Value;
        var attributes = ValidAttributes() with
        {
            Login = Email.Parse("ivan@example.com").Value,
            Email = contactEmail,
        };

        // Act
        var user = User.Create(attributes).Value;

        // Assert
        user.Email.Should().Be(contactEmail);
    }

    /// <summary>
    /// An explicitly given contact phone wins over a phone login.
    /// </summary>
    [Test]
    public void Create_PhoneLoginWithExplicitPhoneNumber_KeepsExplicitPhoneNumber()
    {
        // Arrange
        var contactPhone = PhoneNumber.Parse("+12025550142").Value;
        var attributes = ValidAttributes() with
        {
            Login = PhoneNumber.Parse("+77011234567").Value,
            PhoneNumber = contactPhone,
        };

        // Act
        var user = User.Create(attributes).Value;

        // Assert
        user.PhoneNumber.Should().Be(contactPhone);
    }

    /// <summary>
    /// With a username login, explicitly given contact email and phone are both kept.
    /// </summary>
    [Test]
    public void Create_UsernameLoginWithContacts_KeepsContacts()
    {
        // Arrange
        var email = Email.Parse("ivan@example.com").Value;
        var phone = PhoneNumber.Parse("+77011234567").Value;

        // Act
        var user = User.Create(ValidAttributes() with { Email = email, PhoneNumber = phone }).Value;

        // Assert
        user.Email.Should().Be(email);
        user.PhoneNumber.Should().Be(phone);
    }

    /// <summary>
    /// Equal dedicated accesses passed more than once are stored once.
    /// </summary>
    [Test]
    public void Create_DuplicateDedicatedAccess_IsDeduplicated()
    {
        // Arrange
        var attributes = ValidAttributes() with { DedicatedAccess = AccessesOf("bins:read,write", "bins:write,read") };

        // Act
        var user = User.Create(attributes).Value;

        // Assert
        user.DedicatedAccess.Should().ContainSingle();
    }

    /// <summary>
    /// An identity id of exactly 2048 characters, the maximum, is accepted.
    /// </summary>
    [Test]
    public void Create_IdentityIdOf2048Characters_Succeeds()
    {
        // Arrange
        var attributes = ValidAttributes() with { IdentityId = new string('a', 2048) };

        // Act
        var result = User.Create(attributes);

        // Assert
        result.IsError.Should().BeFalse();
    }

    /// <summary>
    /// An empty or whitespace-only identity id returns a single <see cref="UserErrors.InvalidIdentityId"/>.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public void Create_EmptyIdentityId_ReturnsInvalidIdentityId(string identityId)
    {
        // Arrange
        var attributes = ValidAttributes() with { IdentityId = identityId };

        // Act
        var result = User.Create(attributes);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Should().Be(UserErrors.InvalidIdentityId);
    }

    /// <summary>
    /// An identity id one character over the 2048-character maximum returns a single
    /// <see cref="UserErrors.InvalidIdentityId"/>.
    /// </summary>
    [Test]
    public void Create_IdentityIdOver2048Characters_ReturnsInvalidIdentityId()
    {
        // Arrange
        var attributes = ValidAttributes() with { IdentityId = new string('a', 2049) };

        // Act
        var result = User.Create(attributes);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Should().Be(UserErrors.InvalidIdentityId);
    }

    /// <summary>
    /// <see cref="Guid.Empty"/> as the role id returns a single <see cref="UserErrors.EmptyRoleId"/>;
    /// "no role" must be expressed as <see langword="null"/>.
    /// </summary>
    [Test]
    public void Create_EmptyRoleId_ReturnsEmptyRoleId()
    {
        // Arrange
        var attributes = ValidAttributes() with { RoleId = Guid.Empty };

        // Act
        var result = User.Create(attributes);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Should().Be(UserErrors.EmptyRoleId);
    }

    /// <summary>
    /// A user without dedicated access returns a single <see cref="UserErrors.AtLeastOneAccessShouldBeDefined"/>.
    /// </summary>
    [Test]
    public void Create_NoDedicatedAccess_ReturnsAtLeastOneAccessShouldBeDefined()
    {
        // Arrange
        var attributes = ValidAttributes() with { DedicatedAccess = [] };

        // Act
        var result = User.Create(attributes);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Should().Be(UserErrors.AtLeastOneAccessShouldBeDefined);
    }

    /// <summary>
    /// When the identity id, the role id and the dedicated access are all invalid,
    /// all three errors are returned together.
    /// </summary>
    [Test]
    public void Create_EverythingInvalid_ReturnsAllErrors()
    {
        // Arrange
        var attributes = ValidAttributes() with { IdentityId = " ", RoleId = Guid.Empty, DedicatedAccess = [] };

        // Act
        var result = User.Create(attributes);

        // Assert
        result.Errors.Should().BeEquivalentTo(new[]
        {
            UserErrors.InvalidIdentityId,
            UserErrors.EmptyRoleId,
            UserErrors.AtLeastOneAccessShouldBeDefined,
        });
    }

    /// <summary>
    /// <see cref="User.CreateRoot"/> creates a root user with no role and no dedicated access,
    /// trims the identity id, and uses an email login as the contact email.
    /// </summary>
    [Test]
    public void CreateRoot_EmailLogin_ReturnsRootWithoutRoleOrAccess()
    {
        // Arrange
        var login = LoginOf("root@example.com");

        // Act
        var result = User.CreateRoot(login, "  identity-root  ", HumanNameOf("Root"));

        // Assert
        result.IsError.Should().BeFalse();
        var root = result.Value;
        root.IsRoot.Should().BeTrue();
        root.Login.Should().Be(login);
        root.Email.Should().Be(login);
        root.PhoneNumber.Should().BeNull();
        root.IdentityId.Should().Be("identity-root");
        root.RoleId.Should().BeNull();
        root.DedicatedAccess.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="User.CreateRoot"/> uses a phone login as the contact phone.
    /// </summary>
    [Test]
    public void CreateRoot_PhoneLogin_SetsContactPhoneNumber()
    {
        // Arrange
        var login = LoginOf("+77011234567");

        // Act
        var root = User.CreateRoot(login, "identity-root", HumanNameOf("Root")).Value;

        // Assert
        root.PhoneNumber.Should().Be(login);
        root.Email.Should().BeNull();
    }

    /// <summary>
    /// <see cref="User.CreateRoot"/> with a username login leaves the contact email and phone empty.
    /// </summary>
    [Test]
    public void CreateRoot_UsernameLogin_HasNoContacts()
    {
        // Arrange
        var login = LoginOf("root");

        // Act
        var root = User.CreateRoot(login, "identity-root", HumanNameOf("Root")).Value;

        // Assert
        root.Email.Should().BeNull();
        root.PhoneNumber.Should().BeNull();
    }

    /// <summary>
    /// <see cref="User.CreateRoot"/> with an empty or whitespace-only identity id returns
    /// <see cref="UserErrors.InvalidIdentityId"/>.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public void CreateRoot_EmptyIdentityId_ReturnsInvalidIdentityId(string identityId)
    {
        // Act
        var result = User.CreateRoot(LoginOf(), identityId, HumanNameOf());

        // Assert
        result.FirstError.Should().Be(UserErrors.InvalidIdentityId);
    }

    /// <summary>
    /// <see cref="User.CreateRoot"/> with an identity id over 2048 characters returns
    /// <see cref="UserErrors.InvalidIdentityId"/>.
    /// </summary>
    [Test]
    public void CreateRoot_IdentityIdOver2048Characters_ReturnsInvalidIdentityId()
    {
        // Arrange
        var identityId = new string('a', 2049);

        // Act
        var result = User.CreateRoot(LoginOf(), identityId, HumanNameOf());

        // Assert
        result.FirstError.Should().Be(UserErrors.InvalidIdentityId);
    }

    /// <summary>
    /// <see cref="User.Rename"/> replaces the name and refreshes <see cref="User.UpdatedAt"/>.
    /// </summary>
    [Test]
    public void Rename_ReplacesName()
    {
        // Arrange
        var user = CreateUser();
        var updatedBefore = user.UpdatedAt;
        var newName = HumanNameOf("Petr Petrov");

        // Act
        var result = user.Rename(newName);

        // Assert
        result.Value.Should().Be(Result.Updated);
        user.Name.Should().Be(newName);
        user.UpdatedAt.Should().BeOnOrAfter(updatedBefore);
    }

    /// <summary>
    /// <see cref="User.UpdateRole"/> assigns the role and returns the dedicated access merged
    /// with the role's accesses, one entry per resource.
    /// </summary>
    [Test]
    public void UpdateRole_NewRole_AssignsRoleAndReturnsMergedAccess()
    {
        // Arrange
        var user = CreateUser(dedicatedAccess: "bins:read");
        var role = RoleWith("bins:write", "users:read");

        // Act
        var result = user.UpdateRole(role);

        // Assert
        user.RoleId.Should().Be(role.Id);
        result.Value.Should().BeEquivalentTo(AccessesOf("bins:read,write", "users:read"));
    }

    /// <summary>
    /// <see cref="User.UpdateRole"/> with <see langword="null"/> clears the role and returns only the dedicated access.
    /// </summary>
    [Test]
    public void UpdateRole_Null_ClearsRoleAndReturnsDedicatedAccess()
    {
        // Arrange
        var role = RoleWith("users:read");
        var user = CreateUser(role.Id, "bins:read");

        // Act
        var result = user.UpdateRole(null);

        // Assert
        user.RoleId.Should().BeNull();
        result.Value.Should().BeEquivalentTo(AccessesOf("bins:read"));
    }

    /// <summary>
    /// The root's role cannot be changed: <see cref="User.UpdateRole"/> returns the forbidden
    /// <see cref="UserErrors.RootAccessIsImmutable"/> and leaves the root untouched.
    /// </summary>
    [Test]
    public void UpdateRole_Root_ReturnsRootAccessIsImmutable()
    {
        // Arrange
        var root = CreateRoot();
        var updatedBefore = root.UpdatedAt;

        // Act
        var result = root.UpdateRole(RoleWith());

        // Assert
        result.FirstError.Should().Be(UserErrors.RootAccessIsImmutable);
        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
        root.RoleId.Should().BeNull();
        root.UpdatedAt.Should().Be(updatedBefore);
    }

    /// <summary>
    /// <see cref="User.UpdateDedicatedAccess"/> replaces (not extends) the dedicated access and returns it
    /// merged with the current role's accesses.
    /// </summary>
    [Test]
    public void UpdateDedicatedAccess_ValidAccess_ReplacesItAndReturnsMergedAccess()
    {
        // Arrange
        var role = RoleWith("bins:read");
        var user = CreateUser(role.Id, "users:read");

        // Act
        var result = user.UpdateDedicatedAccess(AccessesOf("bins:write", "alerts:read"), role);

        // Assert
        user.DedicatedAccess.Should().BeEquivalentTo(AccessesOf("bins:write", "alerts:read"));
        result.Value.Should().BeEquivalentTo(AccessesOf("bins:read,write", "alerts:read"));
    }

    /// <summary>
    /// For a user without a role, <see cref="User.UpdateDedicatedAccess"/> returns just the new dedicated access.
    /// </summary>
    [Test]
    public void UpdateDedicatedAccess_WithoutRole_ReturnsDedicatedAccess()
    {
        // Arrange
        var user = CreateUser(dedicatedAccess: "users:read");

        // Act
        var result = user.UpdateDedicatedAccess(AccessesOf("bins:read"), null);

        // Assert
        result.Value.Should().BeEquivalentTo(AccessesOf("bins:read"));
    }

    /// <summary>
    /// <see cref="User.UpdateDedicatedAccess"/> with no access returns
    /// <see cref="UserErrors.AtLeastOneAccessShouldBeDefined"/> and leaves the user untouched.
    /// </summary>
    [Test]
    public void UpdateDedicatedAccess_NoAccess_ReturnsErrorAndKeepsState()
    {
        // Arrange
        var user = CreateUser(dedicatedAccess: "users:read");
        var updatedBefore = user.UpdatedAt;

        // Act
        var result = user.UpdateDedicatedAccess([], null);

        // Assert
        result.FirstError.Should().Be(UserErrors.AtLeastOneAccessShouldBeDefined);
        user.DedicatedAccess.Should().BeEquivalentTo(AccessesOf("users:read"));
        user.UpdatedAt.Should().Be(updatedBefore);
    }

    /// <summary>
    /// Passing a role other than the assigned one to <see cref="User.UpdateDedicatedAccess"/> returns
    /// <see cref="UserErrors.RoleMismatch"/> and leaves the dedicated access untouched.
    /// </summary>
    [Test]
    public void UpdateDedicatedAccess_RoleOtherThanAssigned_ReturnsRoleMismatchAndKeepsState()
    {
        // Arrange: the user is assigned one role, and a different role is passed in
        var assignedRoleId = Guid.NewGuid();
        var user = CreateUser(assignedRoleId, "users:read");
        var otherRole = RoleWith();

        // Act
        var result = user.UpdateDedicatedAccess(AccessesOf("bins:read"), otherRole);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Should().Be(UserErrors.RoleMismatch(assignedRoleId.ToString(), otherRole.Id.ToString()));
        user.DedicatedAccess.Should().BeEquivalentTo(AccessesOf("users:read"));
    }

    /// <summary>
    /// The root's dedicated access cannot be changed: <see cref="User.UpdateDedicatedAccess"/> returns
    /// <see cref="UserErrors.RootAccessIsImmutable"/> and the root keeps no dedicated access.
    /// </summary>
    [Test]
    public void UpdateDedicatedAccess_Root_ReturnsRootAccessIsImmutable()
    {
        // Arrange
        var root = CreateRoot();

        // Act
        var result = root.UpdateDedicatedAccess(AccessesOf("bins:read"), null);

        // Assert
        result.FirstError.Should().Be(UserErrors.RootAccessIsImmutable);
        root.DedicatedAccess.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="User.ResolveFullAccess"/> unites the dedicated access with the assigned role's accesses,
    /// one entry per resource.
    /// </summary>
    [Test]
    public void ResolveFullAccess_AssignedRole_MergesOneEntryPerResource()
    {
        // Arrange
        var role = RoleWith("bins:write", "users:read");
        var user = CreateUser(role.Id, "bins:read", "alerts:read");

        // Act
        var result = user.ResolveFullAccess(role);

        // Assert
        result.Value.Should().BeEquivalentTo(AccessesOf("bins:read,write", "users:read", "alerts:read"));
    }

    /// <summary>
    /// For a user without a role, <see cref="User.ResolveFullAccess"/> returns the dedicated access.
    /// </summary>
    [Test]
    public void ResolveFullAccess_NoRole_ReturnsDedicatedAccess()
    {
        // Arrange
        var user = CreateUser(dedicatedAccess: "bins:read,write");

        // Act
        var result = user.ResolveFullAccess(null);

        // Assert
        result.Value.Should().BeEquivalentTo(AccessesOf("bins:read,write"));
    }

    /// <summary>
    /// Passing a role to a user who has none returns a <see cref="UserErrors.RoleMismatch"/> failure
    /// that shows '-' for the missing assigned role.
    /// </summary>
    [Test]
    public void ResolveFullAccess_RoleGivenButUserHasNone_ReturnsRoleMismatch()
    {
        // Arrange
        var user = CreateUser();
        var role = RoleWith();

        // Act
        var error = user.ResolveFullAccess(role).FirstError;

        // Assert
        error.Should().Be(UserErrors.RoleMismatch(null, role.Id.ToString()));
        error.Description.Should().Be($"Role assigned to the user - doesn't match the requested role {role.Id}");
    }

    /// <summary>
    /// Passing no role for a user who has one returns a <see cref="UserErrors.RoleMismatch"/> failure
    /// that shows '-' for the missing requested role.
    /// </summary>
    [Test]
    public void ResolveFullAccess_NoRoleGivenButUserHasOne_ReturnsRoleMismatch()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var user = CreateUser(roleId);

        // Act
        var error = user.ResolveFullAccess(null).FirstError;

        // Assert
        error.Should().Be(UserErrors.RoleMismatch(roleId.ToString(), null));
        error.Description.Should().Be($"Role assigned to the user {roleId} doesn't match the requested role -");
    }

    /// <summary>
    /// The root has no role and no dedicated access, so <see cref="User.ResolveFullAccess"/> returns an empty set;
    /// the root bypasses access checks instead.
    /// </summary>
    [Test]
    public void ResolveFullAccess_Root_ReturnsEmptySet()
    {
        // Arrange
        var root = CreateRoot();

        // Act
        var result = root.ResolveFullAccess(null);

        // Assert
        result.Value.Should().BeEmpty();
    }
}
