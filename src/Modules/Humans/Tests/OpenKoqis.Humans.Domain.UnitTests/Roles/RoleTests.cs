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

    [Test]
    public void Create_ValidAttributes_ReturnsRole()
    {
        var attributes = ValidAttributes() with { Name = "  Operators  " };

        var result = Role.Create(attributes);

        result.IsError.Should().BeFalse();
        var role = result.Value;
        role.Id.Should().NotBeEmpty();
        role.Name.Should().Be("Operators");
        role.CreatedBy.Should().Be(attributes.CreatedBy);
        role.Accesses.Should().BeEquivalentTo(attributes.Accesses);
        role.UpdatedAt.Should().BeOnOrAfter(role.CreatedAt);
    }

    [Test]
    public void Create_DuplicateAccesses_AreDeduplicated()
    {
        var result = Role.Create(ValidAttributes() with { Accesses = AccessesOf("bins:read,write", "bins:write,read") });

        result.Value.Accesses.Should().ContainSingle()
            .Which.Should().Be(AccessOf("bins:read,write"));
    }

    [Test]
    public void Create_TwoRoles_GetDistinctIds()
        => Role.Create(ValidAttributes()).Value.Id.Should().NotBe(Role.Create(ValidAttributes()).Value.Id);

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("Sales Manager")]
    [Arguments("Admin1")]
    [Arguments("super-admin")]
    [Arguments("Abcdefghijklmnopq")]
    public void Create_InvalidName_ReturnsInvalidName(string name)
        => Role.Create(ValidAttributes() with { Name = name }).Errors.Should().ContainSingle()
            .Which.Should().Be(RoleErrors.InvalidName);

    [Test]
    public void Create_EmptyCreator_ReturnsCouldntDetermineCreator()
        => Role.Create(ValidAttributes() with { CreatedBy = Guid.Empty }).Errors.Should().ContainSingle()
            .Which.Should().Be(RoleErrors.CouldntDetermineCreator);

    [Test]
    public void Create_NoAccesses_ReturnsAtLeastOneAccessShouldBeDefined()
        => Role.Create(ValidAttributes() with { Accesses = [] }).Errors.Should().ContainSingle()
            .Which.Should().Be(RoleErrors.AtLeastOneAccessShouldBeDefined);

    [Test]
    public void Create_EverythingInvalid_ReturnsAllErrors()
    {
        var result = Role.Create(new Role.CreationAttributes
        {
            Name = "",
            Accesses = [],
            CreatedBy = Guid.Empty,
        });

        result.Errors.Should().BeEquivalentTo(new[] { RoleErrors.CouldntDetermineCreator, RoleErrors.InvalidName, RoleErrors.AtLeastOneAccessShouldBeDefined });
    }

    [Test]
    public void Rename_ValidName_ReplacesTrimmedName()
    {
        var role = RoleWith();
        var updatedBefore = role.UpdatedAt;

        var result = role.Rename("  Managers ");

        result.Value.Should().Be(Result.Updated);
        role.Name.Should().Be("Managers");
        role.UpdatedAt.Should().BeOnOrAfter(updatedBefore);
    }

    [Test]
    [Arguments("")]
    [Arguments("Sales Manager")]
    [Arguments("Admin1")]
    public void Rename_InvalidName_ReturnsErrorAndKeepsState(string name)
    {
        var role = RoleWith();
        var updatedBefore = role.UpdatedAt;

        var result = role.Rename(name);

        result.FirstError.Should().Be(RoleErrors.InvalidName);
        role.Name.Should().Be("Operators");
        role.UpdatedAt.Should().Be(updatedBefore);
    }

    [Test]
    public void GrantAccess_NewAccess_IsAdded()
    {
        var role = RoleWith("bins:read");

        var result = role.GrantAccess(AccessesOf("users:read"));

        result.Value.Should().Be(Result.Updated);
        role.Accesses.Should().BeEquivalentTo(AccessesOf("bins:read", "users:read"));
    }

    [Test]
    public void GrantAccess_AccessAlreadyPresent_IsIgnored()
    {
        var role = RoleWith("bins:read,write");

        role.GrantAccess(AccessesOf("bins:write,read"));

        role.Accesses.Should().ContainSingle()
            .Which.Should().Be(AccessOf("bins:read,write"));
    }

    [Test]
    public void RevokeAccess_WholeAccess_IsRemoved()
    {
        var role = RoleWith("bins:read,write", "users:read");

        var result = role.RevokeAccess(AccessesOf("bins:read,write"));

        result.Value.Should().Be(Result.Updated);
        role.Accesses.Should().BeEquivalentTo(AccessesOf("users:read"));
    }

    [Test]
    public void RevokeAccess_SomePermissionsOfAccess_RemovesOnlyThosePermissions()
    {
        var role = RoleWith("bins:read,write,delete", "users:read");

        role.RevokeAccess(AccessesOf("bins:write,delete"));

        role.Accesses.Should().BeEquivalentTo(AccessesOf("bins:read", "users:read"));
    }

    [Test]
    public void RevokeAccess_PermissionsAcrossSeveralResources_AreRemoved()
    {
        var role = RoleWith("bins:read,write", "users:read,write");

        role.RevokeAccess(AccessesOf("bins:write", "users:read"));

        role.Accesses.Should().BeEquivalentTo(AccessesOf("bins:read", "users:write"));
    }

    [Test]
    public void RevokeAccess_AccessNotGranted_LeavesAccessesUnchanged()
    {
        var role = RoleWith("bins:read,write");

        role.RevokeAccess(AccessesOf("bins:delete", "users:read"));

        role.Accesses.Should().BeEquivalentTo(AccessesOf("bins:read,write"));
    }

    [Test]
    public void Equals_SameInstance_ReturnsTrue()
    {
        var role = RoleWith();

        role.Should().Be(role);
    }

    [Test]
    public void Equals_RolesWithSameContent_AreDistinct()
        => RoleWith("bins:read").Should().NotBe(RoleWith("bins:read"));
}
