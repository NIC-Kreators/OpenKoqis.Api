using AwesomeAssertions;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Humans.Domain.Users.Types;
using static OpenKoqis.Humans.Application.UnitTests.Fixtures;

namespace OpenKoqis.Humans.Application.UnitTests.Dto;

public class UserDtoTests
{
    /// <summary>
    /// A regular user maps every field, including contacts, role id, dedicated accesses and the name of the given role.
    /// </summary>
    [Test]
    public void FromUser_RegularUser_MapsAllFields()
    {
        // Arrange
        var role = RoleWith();
        var user = User.Create(new User.CreationAttributes
        {
            Login = LoginOf("ivan"),
            IdentityId = "identity-1",
            Name = HumanNameOf("Ivan Ivanov"),
            Email = Email.Parse("ivan@example.com").Value,
            PhoneNumber = PhoneNumber.Parse("+77011234567").Value,
            RoleId = role.Id,
            DedicatedAccess = AccessesOf("bins:read", "alerts:read"),
        }).Value;

        // Act
        var dto = UserDto.FromUser(user, role);

        // Assert
        dto.Id.Should().Be(user.Id);
        dto.IdentityId.Should().Be("identity-1");
        dto.Login.Should().Be(user.Login.Value);
        dto.Email.Should().Be("ivan@example.com");
        dto.PhoneNumber.Should().Be(user.PhoneNumber!.Value);
        dto.FullName.Should().Be(user.Name.FullName);
        dto.IsRoot.Should().BeFalse();
        dto.RoleId.Should().Be(role.Id);
        dto.RoleName.Should().Be(role.Name);
        dto.DedicatedAccess.Should().BeEquivalentTo(user.DedicatedAccess.Select(a => a.ToString()));
        dto.DedicatedAccess.Should().HaveCount(2);
        dto.LocationId.Should().Be(user.LocationId);
        dto.DeletedAt.Should().BeNull();
    }

    /// <summary>
    /// The root user maps as root with no role and no dedicated accesses.
    /// </summary>
    [Test]
    public void FromUser_RootUser_MapsAsRootWithoutRole()
    {
        // Arrange
        var root = RootUser();

        // Act
        var dto = UserDto.FromUser(root, role: null);

        // Assert
        dto.IsRoot.Should().BeTrue();
        dto.RoleName.Should().BeNull();
        dto.RoleId.Should().BeNull();
        dto.DedicatedAccess.Should().BeEmpty();
    }

    /// <summary>
    /// A soft-deleted user carries its DeletedAt into the DTO.
    /// </summary>
    [Test]
    public void FromUser_DeletedUser_MapsDeletedAt()
    {
        // Arrange
        var user = RegularUser();
        user.Delete();

        // Act
        var dto = UserDto.FromUser(user, role: null);

        // Assert
        user.DeletedAt.Should().NotBeNull();
        dto.DeletedAt.Should().Be(user.DeletedAt);
    }

    /// <summary>
    /// A user whose role no longer exists keeps its RoleId and maps with a null RoleName.
    /// </summary>
    [Test]
    public void FromUser_AssignedRoleMissing_MapsRoleIdWithoutRoleName()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var user = RegularUser(roleId: roleId);

        // Act
        var dto = UserDto.FromUser(user, role: null);

        // Assert
        dto.RoleId.Should().Be(roleId);
        dto.RoleName.Should().BeNull();
    }

    /// <summary>
    /// Passing a role other than the one the user is assigned to is a caller bug and throws.
    /// </summary>
    [Test]
    public void FromUser_RoleDiffersFromAssignedRole_Throws()
    {
        // Arrange
        var user = RegularUser(roleId: Guid.NewGuid());
        var otherRole = RoleWith();

        // Act
        var act = () => UserDto.FromUser(user, otherRole);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("role");
    }

    /// <summary>
    /// Passing a role for a user that has no role assigned is a caller bug and throws.
    /// </summary>
    [Test]
    public void FromUser_RoleGivenForUserWithoutRole_Throws()
    {
        // Arrange
        var user = RegularUser();
        var role = RoleWith();

        // Act
        var act = () => UserDto.FromUser(user, role);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("role");
    }
}
