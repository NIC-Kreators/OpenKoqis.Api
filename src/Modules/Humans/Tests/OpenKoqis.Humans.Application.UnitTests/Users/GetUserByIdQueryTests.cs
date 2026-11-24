using AwesomeAssertions;
using ErrorOr;
using NSubstitute;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Users;
using OpenKoqis.Humans.Application.Features.Users.Errors;
using OpenKoqis.Humans.Application.Features.Users.Queries;

namespace OpenKoqis.Humans.Application.UnitTests.Users;

public class GetUserByIdQueryTests
{
    private readonly IUserReadStore _store = Substitute.For<IUserReadStore>();

    private GetUserByIdQueryHandler CreateHandler() => new(_store);

    private static UserDto DtoOf(Guid id, DateTime? deletedAt = null) => new()
    {
        Id = id,
        IdentityId = "identity-1",
        Login = "ivan",
        FullName = "Ivan Ivanov",
        IsRoot = false,
        DedicatedAccess = ["bins:read"],
        DeletedAt = deletedAt,
    };

    /// <summary>
    /// An existing user is returned exactly as the store provides it, and the store is asked once for the request id.
    /// </summary>
    [Test]
    public async Task Handle_UserExists_ReturnsStoreDtoUnchanged()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = DtoOf(id);
        _store.GetUserById(id, Arg.Any<CancellationToken>()).Returns(dto);

        // Act
        var result = await CreateHandler().Handle(new GetUserByIdQueryRequest { Id = id }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeSameAs(dto);
        await _store.Received(1).GetUserById(id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A user the store does not find yields a not-found error for that id.
    /// </summary>
    [Test]
    public async Task Handle_UserMissing_ReturnsNotFoundError()
    {
        // Arrange
        var id = Guid.NewGuid();
        _store.GetUserById(id, Arg.Any<CancellationToken>()).Returns((UserDto?)null);

        // Act
        var result = await CreateHandler().Handle(new GetUserByIdQueryRequest { Id = id }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(UserFeatureErrors.NotFound(id).Code);
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    /// <summary>
    /// A soft-deleted user is returned as the store provides it, with its deletion time preserved.
    /// </summary>
    [Test]
    public async Task Handle_UserSoftDeleted_ReturnsDtoWithDeletedAt()
    {
        // Arrange
        var id = Guid.NewGuid();
        var deletedAt = new DateTime(2026, 5, 1, 10, 30, 0, DateTimeKind.Utc);
        _store.GetUserById(id, Arg.Any<CancellationToken>()).Returns(DtoOf(id, deletedAt));

        // Act
        var result = await CreateHandler().Handle(new GetUserByIdQueryRequest { Id = id }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.DeletedAt.Should().Be(deletedAt);
    }
}
