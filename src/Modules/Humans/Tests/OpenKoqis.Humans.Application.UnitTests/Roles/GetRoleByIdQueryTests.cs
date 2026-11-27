using AwesomeAssertions;
using ErrorOr;
using NSubstitute;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Roles;
using OpenKoqis.Humans.Application.Features.Roles.Errors;
using OpenKoqis.Humans.Application.Features.Roles.Queries;

namespace OpenKoqis.Humans.Application.UnitTests.Roles;

public class GetRoleByIdQueryTests
{
    private readonly IRoleReadStore _store = Substitute.For<IRoleReadStore>();

    private GetRoleByIdQueryHandler CreateHandler() => new(_store);

    private static RoleSummaryDto DtoOf(Guid id, int usersCount = 0) => new()
    {
        Id = id,
        Name = "Manager",
        Accesses = ["bins:read,write"],
        CreatedBy = Guid.NewGuid(),
        CreatedAt = new DateTime(2026, 5, 1, 10, 30, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 5, 2, 10, 30, 0, DateTimeKind.Utc),
        UsersCount = usersCount,
    };

    /// <summary>
    /// An existing role is returned exactly as the store provides it, and the store is asked once for the request id.
    /// </summary>
    [Test]
    public async Task Handle_RoleExists_ReturnsStoreDtoUnchanged()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = DtoOf(id);
        _store.GetRoleById(id, Arg.Any<CancellationToken>()).Returns(dto);

        // Act
        var result = await CreateHandler().Handle(new GetRoleByIdQueryRequest { Id = id }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeSameAs(dto);
        await _store.Received(1).GetRoleById(id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A role the store does not find yields a not-found error for that id.
    /// </summary>
    [Test]
    public async Task Handle_RoleMissing_ReturnsNotFoundError()
    {
        // Arrange
        var id = Guid.NewGuid();
        _store.GetRoleById(id, Arg.Any<CancellationToken>()).Returns((RoleSummaryDto?)null);

        // Act
        var result = await CreateHandler().Handle(new GetRoleByIdQueryRequest { Id = id }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(RoleFeatureErrors.NotFound(id).Code);
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    /// <summary>
    /// A role assigned to users is returned with the users count the store reports.
    /// </summary>
    [Test]
    public async Task Handle_RoleWithUsers_KeepsUsersCount()
    {
        // Arrange
        var id = Guid.NewGuid();
        _store.GetRoleById(id, Arg.Any<CancellationToken>()).Returns(DtoOf(id, usersCount: 3));

        // Act
        var result = await CreateHandler().Handle(new GetRoleByIdQueryRequest { Id = id }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.UsersCount.Should().Be(3);
    }
}
