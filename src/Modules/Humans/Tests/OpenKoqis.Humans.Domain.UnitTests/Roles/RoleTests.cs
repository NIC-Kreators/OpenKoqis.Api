using AwesomeAssertions;
using ErrorOr;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Roles.Errors;
using static OpenKoqis.Humans.Domain.UnitTests.Fixtures;

namespace OpenKoqis.Humans.Domain.UnitTests.Roles;

public class RoleTests
{
    private static Role.CreationAttributes ValidAttributes() => new()
    {
        Name = "Operators",
        Accesses = AccessesOf("bins:read,write", "users:read"),
        CreatedBy = Guid.NewGuid(),
    };

    /// <summary>
    /// Valid attributes create a role with a fresh id, the trimmed name, the given creator and accesses,
    /// and timestamps set at creation.
    /// </summary>
    [Test]
    public void Create_ValidAttributes_ReturnsRole()
    {
        // Arrange
        var attributes = ValidAttributes() with { Name = "  Operators  " };

        // Act
        var result = Role.Create(attributes);

        // Assert
        result.IsError.Should().BeFalse();
        var role = result.Value;
        role.Id.Should().NotBeEmpty();
        role.Name.Should().Be("Operators");
        role.CreatedBy.Should().Be(attributes.CreatedBy);
        role.Accesses.Should().BeEquivalentTo(attributes.Accesses);
        role.UpdatedAt.Should().BeOnOrAfter(role.CreatedAt);
    }

    /// <summary>
    /// Equal accesses passed more than once are stored once.
    /// </summary>
    [Test]
    public void Create_DuplicateAccesses_AreDeduplicated()
    {
        // Arrange
        var attributes = ValidAttributes() with { Accesses = AccessesOf("bins:read,write", "bins:write,read") };

        // Act
        var result = Role.Create(attributes);

        // Assert
        result.Value.Accesses.Should().ContainSingle()
            .Which.Should().Be(AccessOf("bins:read,write"));
    }

    /// <summary>
    /// Every created role gets its own id, even from identical attributes.
    /// </summary>
    [Test]
    public void Create_TwoRoles_GetDistinctIds()
    {
        // Act
        var first = Role.Create(ValidAttributes()).Value;
        var second = Role.Create(ValidAttributes()).Value;

        // Assert
        first.Id.Should().NotBe(second.Id);
    }

    /// <summary>
    /// A name that is blank, contains anything but letters, or is longer than 16 letters
    /// returns a single <see cref="RoleErrors.InvalidName"/>.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("Sales Manager")]
    [Arguments("Admin1")]
    [Arguments("super-admin")]
    [Arguments("Abcdefghijklmnopq")]
    public void Create_InvalidName_ReturnsInvalidName(string name)
    {
        // Arrange
        var attributes = ValidAttributes() with { Name = name };

        // Act
        var result = Role.Create(attributes);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Should().Be(RoleErrors.InvalidName);
    }

    /// <summary>
    /// An empty creator id returns a single <see cref="RoleErrors.CouldntDetermineCreator"/>.
    /// </summary>
    [Test]
    public void Create_EmptyCreator_ReturnsCouldntDetermineCreator()
    {
        // Arrange
        var attributes = ValidAttributes() with { CreatedBy = Guid.Empty };

        // Act
        var result = Role.Create(attributes);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Should().Be(RoleErrors.CouldntDetermineCreator);
    }

    /// <summary>
    /// A role without any access returns a single <see cref="RoleErrors.AtLeastOneAccessShouldBeDefined"/>.
    /// </summary>
    [Test]
    public void Create_NoAccesses_ReturnsAtLeastOneAccessShouldBeDefined()
    {
        // Arrange
        var attributes = ValidAttributes() with { Accesses = [] };

        // Act
        var result = Role.Create(attributes);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Should().Be(RoleErrors.AtLeastOneAccessShouldBeDefined);
    }

    /// <summary>
    /// When the creator, the name and the accesses are all invalid, all three errors are returned together.
    /// </summary>
    [Test]
    public void Create_EverythingInvalid_ReturnsAllErrors()
    {
        // Arrange
        var attributes = new Role.CreationAttributes
        {
            Name = "",
            Accesses = [],
            CreatedBy = Guid.Empty,
        };

        // Act
        var result = Role.Create(attributes);

        // Assert
        result.Errors.Should().BeEquivalentTo(new[]
        {
            RoleErrors.CouldntDetermineCreator,
            RoleErrors.InvalidName,
            RoleErrors.AtLeastOneAccessShouldBeDefined,
        });
    }

    /// <summary>
    /// <see cref="Role.Rename"/> with a valid name stores it trimmed and refreshes <see cref="Role.UpdatedAt"/>.
    /// </summary>
    [Test]
    public void Rename_ValidName_ReplacesTrimmedName()
    {
        // Arrange
        var role = RoleWith();
        var updatedBefore = role.UpdatedAt;

        // Act
        var result = role.Rename("  Managers ");

        // Assert
        result.Value.Should().Be(Result.Updated);
        role.Name.Should().Be("Managers");
        role.UpdatedAt.Should().BeOnOrAfter(updatedBefore);
    }

    /// <summary>
    /// <see cref="Role.Rename"/> with an invalid name returns <see cref="RoleErrors.InvalidName"/>
    /// and leaves the name and <see cref="Role.UpdatedAt"/> untouched.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments("Sales Manager")]
    [Arguments("Admin1")]
    public void Rename_InvalidName_ReturnsErrorAndKeepsState(string name)
    {
        // Arrange
        var role = RoleWith();
        var updatedBefore = role.UpdatedAt;

        // Act
        var result = role.Rename(name);

        // Assert
        result.FirstError.Should().Be(RoleErrors.InvalidName);
        role.Name.Should().Be("Operators");
        role.UpdatedAt.Should().Be(updatedBefore);
    }

    /// <summary>
    /// <see cref="Role.GrantAccess"/> adds an access on a resource the role had no access to.
    /// </summary>
    [Test]
    public void GrantAccess_NewAccess_IsAdded()
    {
        // Arrange
        var role = RoleWith("bins:read");

        // Act
        var result = role.GrantAccess(AccessesOf("users:read"));

        // Assert
        result.Value.Should().Be(Result.Updated);
        role.Accesses.Should().BeEquivalentTo(AccessesOf("bins:read", "users:read"));
    }

    /// <summary>
    /// <see cref="Role.GrantAccess"/> ignores an access equal to one the role already has.
    /// </summary>
    [Test]
    public void GrantAccess_AccessAlreadyPresent_IsIgnored()
    {
        // Arrange
        var role = RoleWith("bins:read,write");

        // Act
        role.GrantAccess(AccessesOf("bins:write,read"));

        // Assert
        role.Accesses.Should().ContainSingle()
            .Which.Should().Be(AccessOf("bins:read,write"));
    }

    /// <summary>
    /// <see cref="Role.GrantAccess"/> with new permissions on a resource the role already has access to
    /// unites them into that resource's single entry.
    /// </summary>
    [Test]
    public void GrantAccess_NewPermissionsOnExistingResource_AreMergedIntoOneEntry()
    {
        // Arrange
        var role = RoleWith("bins:read", "users:read");

        // Act
        role.GrantAccess(AccessesOf("bins:write,delete"));

        // Assert
        role.Accesses.Should().BeEquivalentTo(AccessesOf("bins:read,write,delete", "users:read"));
    }

    /// <summary>
    /// <see cref="Role.RevokeAccess"/> with every permission of an access removes that access entirely.
    /// </summary>
    [Test]
    public void RevokeAccess_WholeAccess_IsRemoved()
    {
        // Arrange
        var role = RoleWith("bins:read,write", "users:read");

        // Act
        var result = role.RevokeAccess(AccessesOf("bins:read,write"));

        // Assert
        result.Value.Should().Be(Result.Updated);
        role.Accesses.Should().BeEquivalentTo(AccessesOf("users:read"));
    }

    /// <summary>
    /// <see cref="Role.RevokeAccess"/> with some of an access's permissions removes only those permissions
    /// and keeps the rest on the same resource.
    /// </summary>
    [Test]
    public void RevokeAccess_SomePermissionsOfAccess_RemovesOnlyThosePermissions()
    {
        // Arrange
        var role = RoleWith("bins:read,write,delete", "users:read");

        // Act
        role.RevokeAccess(AccessesOf("bins:write,delete"));

        // Assert
        role.Accesses.Should().BeEquivalentTo(AccessesOf("bins:read", "users:read"));
    }

    /// <summary>
    /// <see cref="Role.RevokeAccess"/> removes permissions on several resources in one call.
    /// </summary>
    [Test]
    public void RevokeAccess_PermissionsAcrossSeveralResources_AreRemoved()
    {
        // Arrange
        var role = RoleWith("bins:read,write", "users:read,write");

        // Act
        role.RevokeAccess(AccessesOf("bins:write", "users:read"));

        // Assert
        role.Accesses.Should().BeEquivalentTo(AccessesOf("bins:read", "users:write"));
    }

    /// <summary>
    /// Revoking permissions or resources the role never had leaves its accesses as they were.
    /// </summary>
    [Test]
    public void RevokeAccess_AccessNotGranted_LeavesAccessesUnchanged()
    {
        // Arrange
        var role = RoleWith("bins:read,write");

        // Act
        role.RevokeAccess(AccessesOf("bins:delete", "users:read"));

        // Assert
        role.Accesses.Should().BeEquivalentTo(AccessesOf("bins:read,write"));
    }

    /// <summary>
    /// A role is equal to itself.
    /// </summary>
    [Test]
    public void Equals_SameInstance_ReturnsTrue()
    {
        // Arrange
        var role = RoleWith();

        // Act
        var equals = role.Equals(role);

        // Assert
        equals.Should().BeTrue();
    }

    /// <summary>
    /// Roles are compared by id, so two separately created roles with identical content are not equal.
    /// </summary>
    [Test]
    public void Equals_RolesWithSameContent_AreDistinct()
    {
        // Arrange
        var left = RoleWith("bins:read");
        var right = RoleWith("bins:read");

        // Act
        var equals = left.Equals(right);

        // Assert
        equals.Should().BeFalse();
    }
}
