using AwesomeAssertions;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Shared.Kernel.Access;
using static OpenKoqis.Humans.Application.UnitTests.Fixtures;

namespace OpenKoqis.Humans.Application.UnitTests.Dto;

public class RoleDtoTests
{
    /// <summary>
    /// A role maps its identity, name, creator, timestamps and accesses rendered as strings.
    /// </summary>
    [Test]
    public void FromRole_Role_MapsAllFields()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var role = Role.Create(new Role.CreationAttributes
        {
            Name = "Operators",
            Accesses = AccessesOf("bins:read", "users:read"),
            CreatedBy = creatorId,
        }).Value;

        // Act
        var dto = RoleDto.FromRole(role);

        // Assert
        dto.Id.Should().Be(role.Id);
        dto.Name.Should().Be("Operators");
        dto.CreatedBy.Should().Be(creatorId);
        dto.CreatedAt.Should().Be(role.CreatedAt);
        dto.UpdatedAt.Should().Be(role.UpdatedAt);
        dto.Accesses.Should().BeEquivalentTo(role.Accesses.Select(a => a.ToString()));
        dto.Accesses.Should().HaveCount(2);
    }

    /// <summary>
    /// An access with several permissions renders as one entry that parses back to the same access.
    /// </summary>
    [Test]
    public void FromRole_AccessWithSeveralPermissions_RendersAsResourceAndPermissionList()
    {
        // Arrange
        var role = RoleWith("bins:read,write");

        // Act
        var dto = RoleDto.FromRole(role);

        // Assert
        dto.Accesses.Should().ContainSingle();
        Access.Parse(dto.Accesses[0]).Value.Should().Be(AccessOf("bins:read,write"));
    }
}
