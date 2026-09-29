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

    [Test]
    public void Create_ValidAttributes_ReturnsRegularUser()
    {
        var roleId = Guid.NewGuid();
        var attributes = ValidAttributes() with { IdentityId = "  identity-1  ", RoleId = roleId };

        var result = User.Create(attributes);

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

    [Test]
    public void Create_WithoutRole_Succeeds()
        => User.Create(ValidAttributes() with { RoleId = null }).Value.RoleId.Should().BeNull();

    [Test]
    public void Create_TwoUsers_GetDistinctIds()
        => User.Create(ValidAttributes()).Value.Id.Should().NotBe(User.Create(ValidAttributes()).Value.Id);

    [Test]
    public void Create_EmailLogin_DefaultsContactEmailToLogin()
    {
        var login = Email.Parse("ivan@example.com").Value;

        var user = User.Create(ValidAttributes() with { Login = login }).Value;

        user.Email.Should().Be(login);
        user.PhoneNumber.Should().BeNull();
    }

    [Test]
    public void Create_PhoneLogin_DefaultsContactPhoneNumberToLogin()
    {
        var login = PhoneNumber.Parse("+77011234567").Value;

        var user = User.Create(ValidAttributes() with { Login = login }).Value;

        user.PhoneNumber.Should().Be(login);
        user.Email.Should().BeNull();
    }

    [Test]
    public void Create_EmailLoginWithExplicitEmail_KeepsExplicitEmail()
    {
        var contactEmail = Email.Parse("contact@example.com").Value;

        var user = User.Create(ValidAttributes() with
        {
            Login = Email.Parse("ivan@example.com").Value,
            Email = contactEmail,
        }).Value;

        user.Email.Should().Be(contactEmail);
    }

    [Test]
    public void Create_PhoneLoginWithExplicitPhoneNumber_KeepsExplicitPhoneNumber()
    {
        var contactPhone = PhoneNumber.Parse("+12025550142").Value;

        var user = User.Create(ValidAttributes() with
        {
            Login = PhoneNumber.Parse("+77011234567").Value,
            PhoneNumber = contactPhone,
        }).Value;

        user.PhoneNumber.Should().Be(contactPhone);
    }

    [Test]
    public void Create_UsernameLoginWithContacts_KeepsContacts()
    {
        var email = Email.Parse("ivan@example.com").Value;
        var phone = PhoneNumber.Parse("+77011234567").Value;

        var user = User.Create(ValidAttributes() with { Email = email, PhoneNumber = phone }).Value;

        user.Email.Should().Be(email);
        user.PhoneNumber.Should().Be(phone);
    }

    [Test]
    public void Create_DuplicateDedicatedAccess_IsDeduplicated()
        => User.Create(ValidAttributes() with { DedicatedAccess = AccessesOf("bins:read,write", "bins:write,read") })
            .Value.DedicatedAccess.Should().ContainSingle();

    [Test]
    public void Create_IdentityIdOf2048Characters_Succeeds()
        => User.Create(ValidAttributes() with { IdentityId = new string('a', 2048) }).IsError.Should().BeFalse();

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public void Create_EmptyIdentityId_ReturnsInvalidIdentityId(string identityId)
        => User.Create(ValidAttributes() with { IdentityId = identityId }).Errors.Should().ContainSingle()
            .Which.Should().Be(UserErrors.InvalidIdentityId);

    [Test]
    public void Create_IdentityIdOver2048Characters_ReturnsInvalidIdentityId()
        => User.Create(ValidAttributes() with { IdentityId = new string('a', 2049) }).Errors.Should().ContainSingle()
            .Which.Should().Be(UserErrors.InvalidIdentityId);

    [Test]
    public void Create_EmptyRoleId_ReturnsEmptyRoleId()
        => User.Create(ValidAttributes() with { RoleId = Guid.Empty }).Errors.Should().ContainSingle()
            .Which.Should().Be(UserErrors.EmptyRoleId);

    [Test]
    public void Create_NoDedicatedAccess_ReturnsAtLeastOneAccessShouldBeDefined()
        => User.Create(ValidAttributes() with { DedicatedAccess = [] }).Errors.Should().ContainSingle()
            .Which.Should().Be(UserErrors.AtLeastOneAccessShouldBeDefined);

    [Test]
    public void Create_EverythingInvalid_ReturnsAllErrors()
        => User.Create(ValidAttributes() with { IdentityId = " ", RoleId = Guid.Empty, DedicatedAccess = [] })
            .Errors.Should().BeEquivalentTo(new[] { UserErrors.InvalidIdentityId, UserErrors.EmptyRoleId, UserErrors.AtLeastOneAccessShouldBeDefined });

    [Test]
    public void CreateRoot_EmailLogin_ReturnsRootWithoutRoleOrAccess()
    {
        var login = LoginOf("root@example.com");

        var result = User.CreateRoot(login, "  identity-root  ", HumanNameOf("Root"));

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

    [Test]
    public void CreateRoot_PhoneLogin_SetsContactPhoneNumber()
    {
        var login = LoginOf("+77011234567");

        var root = User.CreateRoot(login, "identity-root", HumanNameOf("Root")).Value;

        root.PhoneNumber.Should().Be(login);
        root.Email.Should().BeNull();
    }

    [Test]
    public void CreateRoot_UsernameLogin_HasNoContacts()
    {
        var root = User.CreateRoot(LoginOf("root"), "identity-root", HumanNameOf("Root")).Value;

        root.Email.Should().BeNull();
        root.PhoneNumber.Should().BeNull();
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public void CreateRoot_EmptyIdentityId_ReturnsInvalidIdentityId(string identityId)
        => User.CreateRoot(LoginOf(), identityId, HumanNameOf()).FirstError.Should().Be(UserErrors.InvalidIdentityId);

    [Test]
    public void CreateRoot_IdentityIdOver2048Characters_ReturnsInvalidIdentityId()
        => User.CreateRoot(LoginOf(), new string('a', 2049), HumanNameOf()).FirstError
            .Should().Be(UserErrors.InvalidIdentityId);

    [Test]
    public void Rename_ReplacesName()
    {
        var user = CreateUser();
        var updatedBefore = user.UpdatedAt;
        var newName = HumanNameOf("Petr Petrov");

        var result = user.Rename(newName);

        result.Value.Should().Be(Result.Updated);
        user.Name.Should().Be(newName);
        user.UpdatedAt.Should().BeOnOrAfter(updatedBefore);
    }

    [Test]
    public void UpdateRole_NewRole_AssignsRoleAndReturnsMergedAccess()
    {
        var user = CreateUser(dedicatedAccess: "bins:read");
        var role = RoleWith("bins:write", "users:read");

        var result = user.UpdateRole(role);

        user.RoleId.Should().Be(role.Id);
        result.Value.Should().BeEquivalentTo(AccessesOf("bins:read,write", "users:read"));
    }

    [Test]
    public void UpdateRole_Null_ClearsRoleAndReturnsDedicatedAccess()
    {
        var role = RoleWith("users:read");
        var user = CreateUser(role.Id, "bins:read");

        var result = user.UpdateRole(null);

        user.RoleId.Should().BeNull();
        result.Value.Should().BeEquivalentTo(AccessesOf("bins:read"));
    }

    [Test]
    public void UpdateRole_Root_ReturnsRootAccessIsImmutable()
    {
        var root = CreateRoot();
        var updatedBefore = root.UpdatedAt;

        var result = root.UpdateRole(RoleWith());

        result.FirstError.Should().Be(UserErrors.RootAccessIsImmutable);
        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
        root.RoleId.Should().BeNull();
        root.UpdatedAt.Should().Be(updatedBefore);
    }

    [Test]
    public void UpdateDedicatedAccess_ValidAccess_ReplacesItAndReturnsMergedAccess()
    {
        var role = RoleWith("bins:read");
        var user = CreateUser(role.Id, "users:read");

        var result = user.UpdateDedicatedAccess(AccessesOf("bins:write", "alerts:read"), role);

        user.DedicatedAccess.Should().BeEquivalentTo(AccessesOf("bins:write", "alerts:read"));
        result.Value.Should().BeEquivalentTo(AccessesOf("bins:read,write", "alerts:read"));
    }

    [Test]
    public void UpdateDedicatedAccess_WithoutRole_ReturnsDedicatedAccess()
    {
        var user = CreateUser(dedicatedAccess: "users:read");

        var result = user.UpdateDedicatedAccess(AccessesOf("bins:read"), null);

        result.Value.Should().BeEquivalentTo(AccessesOf("bins:read"));
    }

    [Test]
    public void UpdateDedicatedAccess_NoAccess_ReturnsErrorAndKeepsState()
    {
        var user = CreateUser(dedicatedAccess: "users:read");
        var updatedBefore = user.UpdatedAt;

        var result = user.UpdateDedicatedAccess([], null);

        result.FirstError.Should().Be(UserErrors.AtLeastOneAccessShouldBeDefined);
        user.DedicatedAccess.Should().BeEquivalentTo(AccessesOf("users:read"));
        user.UpdatedAt.Should().Be(updatedBefore);
    }

    [Test]
    public void UpdateDedicatedAccess_RoleOtherThanAssigned_ReturnsRoleMismatchAndKeepsState()
    {
        var user = CreateUser(Guid.NewGuid(), "users:read");

        var result = user.UpdateDedicatedAccess(AccessesOf("bins:read"), RoleWith());

        result.FirstError.Code.Should().Be("User.RoleMismatch");
        user.DedicatedAccess.Should().BeEquivalentTo(AccessesOf("users:read"));
    }

    [Test]
    public void UpdateDedicatedAccess_Root_ReturnsRootAccessIsImmutable()
    {
        var root = CreateRoot();

        var result = root.UpdateDedicatedAccess(AccessesOf("bins:read"), null);

        result.FirstError.Should().Be(UserErrors.RootAccessIsImmutable);
        root.DedicatedAccess.Should().BeEmpty();
    }

    [Test]
    public void ResolveFullAccess_AssignedRole_MergesOneEntryPerResource()
    {
        var role = RoleWith("bins:write", "users:read");
        var user = CreateUser(role.Id, "bins:read", "alerts:read");

        var result = user.ResolveFullAccess(role);

        result.Value.Should().BeEquivalentTo(AccessesOf("bins:read,write", "users:read", "alerts:read"));
    }

    [Test]
    public void ResolveFullAccess_NoRole_ReturnsDedicatedAccess()
    {
        var user = CreateUser(dedicatedAccess: "bins:read,write");

        user.ResolveFullAccess(null).Value.Should().BeEquivalentTo(AccessesOf("bins:read,write"));
    }

    [Test]
    public void ResolveFullAccess_RoleGivenButUserHasNone_ReturnsRoleMismatch()
    {
        var user = CreateUser();
        var role = RoleWith();

        var error = user.ResolveFullAccess(role).FirstError;

        error.Code.Should().Be("User.RoleMismatch");
        error.Type.Should().Be(ErrorType.Failure);
        error.Description.Should().Be($"Role assigned to the user - doesn't match the requested role {role.Id}");
    }

    [Test]
    public void ResolveFullAccess_NoRoleGivenButUserHasOne_ReturnsRoleMismatch()
    {
        var roleId = Guid.NewGuid();
        var user = CreateUser(roleId);

        var error = user.ResolveFullAccess(null).FirstError;

        error.Code.Should().Be("User.RoleMismatch");
        error.Description.Should().Be($"Role assigned to the user {roleId} doesn't match the requested role -");
    }

    [Test]
    public void ResolveFullAccess_Root_ReturnsEmptySet()
        => CreateRoot().ResolveFullAccess(null).Value.Should().BeEmpty();
}
