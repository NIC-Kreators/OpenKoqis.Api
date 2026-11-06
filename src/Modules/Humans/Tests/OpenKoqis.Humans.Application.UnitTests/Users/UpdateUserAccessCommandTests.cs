using AwesomeAssertions;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Roles;
using OpenKoqis.Humans.Application.Features.Users;
using OpenKoqis.Humans.Application.Features.Users.Commands;
using OpenKoqis.Humans.Application.Features.Users.Errors;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Users;
using static OpenKoqis.Humans.Application.UnitTests.Fixtures;

namespace OpenKoqis.Humans.Application.UnitTests.Users;

public class UpdateUserAccessCommandTests
{
    private readonly IHumansUnitOfWork _unitOfWork = Substitute.For<IHumansUnitOfWork>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IAccessProjection _accessProjection = Substitute.For<IAccessProjection>();

    private UpdateUserAccessCommandHandler CreateHandler() => new(
        _unitOfWork,
        _userRepository,
        _roleRepository,
        _accessProjection,
        NullLogger<UpdateUserAccessCommandHandler>.Instance);

    private void SetupUser(User? user)
        => _userRepository.GetUserWithId(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(user);

    private void SetupRole(Role? role)
        => _roleRepository.GetRoleWithId(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(role);

    private void SetupSaveSucceeds()
        => _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success.ToErrorOr());

    private void SetupProjectionSucceeds()
        => _accessProjection.UserAccessChanged(Arg.Any<User>(), Arg.Any<Role?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success.ToErrorOr());

    private void AssertNoPortCalled()
    {
        _userRepository.ReceivedCalls().Should().BeEmpty();
        _roleRepository.ReceivedCalls().Should().BeEmpty();
        _unitOfWork.ReceivedCalls().Should().BeEmpty();
        _accessProjection.ReceivedCalls().Should().BeEmpty();
    }

    private static UpdateUserAccessCommandRequest RequestFor(
        Guid id,
        Guid? roleId = null,
        params string[] dedicatedAccess)
        => new()
        {
            Id = id,
            RoleId = roleId,
            DedicatedAccess = dedicatedAccess.Length > 0 ? dedicatedAccess : ["users:write"],
        };

    /// <summary>
    /// A valid request with a role replaces the user's role and dedicated access, saves, projects the user with that
    /// role and returns the new access in the response.
    /// </summary>
    [Test]
    public async Task Handle_ValidRequestWithRole_UpdatesUserSavesAndProjectsWithRole()
    {
        // Arrange
        var user = RegularUser(roleId: Guid.NewGuid(), dedicatedAccess: "bins:read");
        var role = RoleWith("bins:read");
        SetupUser(user);
        SetupRole(role);
        SetupSaveSucceeds();
        SetupProjectionSucceeds();
        var request = RequestFor(user.Id, role.Id, "users:write", "reports:read");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        user.RoleId.Should().Be(role.Id);
        user.DedicatedAccess.Should().BeEquivalentTo(AccessesOf("users:write", "reports:read"));
        result.Value.User.RoleId.Should().Be(role.Id);
        result.Value.User.DedicatedAccess.Should().BeEquivalentTo(UserDto.FromUser(user).DedicatedAccess);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _accessProjection.Received(1).UserAccessChanged(user, role, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A request without a role clears the user's existing role and projects the user with a null role.
    /// </summary>
    [Test]
    public async Task Handle_RequestWithoutRole_ClearsRoleAndProjectsWithNull()
    {
        // Arrange
        var user = RegularUser(roleId: Guid.NewGuid(), dedicatedAccess: "bins:read");
        SetupUser(user);
        SetupSaveSucceeds();
        SetupProjectionSucceeds();
        var request = RequestFor(user.Id, null, "users:write");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        user.RoleId.Should().BeNull();
        user.DedicatedAccess.Should().BeEquivalentTo(AccessesOf("users:write"));
        result.Value.User.RoleId.Should().BeNull();
        await _accessProjection.Received(1).UserAccessChanged(user, null, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// An access string that cannot be parsed fails with its access error, and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_InvalidAccessString_ReturnsAccessErrorAndCallsNoPort()
    {
        // Arrange
        var request = RequestFor(Guid.NewGuid(), null, "not-an-access");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("Access.InvalidAccessString");
        AssertNoPortCalled();
    }

    /// <summary>
    /// An empty dedicated access list fails with <c>User.AtLeastOneAccessShouldBeDefined</c>, and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_EmptyAccessList_ReturnsAtLeastOneAccessErrorAndCallsNoPort()
    {
        // Arrange
        var request = new UpdateUserAccessCommandRequest { Id = Guid.NewGuid(), DedicatedAccess = [] };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("User.AtLeastOneAccessShouldBeDefined");
        AssertNoPortCalled();
    }

    /// <summary>
    /// An empty role id fails with <c>User.EmptyRoleId</c>, and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_EmptyRoleId_ReturnsEmptyRoleIdErrorAndCallsNoPort()
    {
        // Arrange
        var request = RequestFor(Guid.NewGuid(), Guid.Empty, "users:write");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("User.EmptyRoleId");
        AssertNoPortCalled();
    }

    /// <summary>
    /// Every invalid input is reported together, and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_SeveralInvalidInputs_ReturnsAllErrorsAndCallsNoPort()
    {
        // Arrange
        var request = RequestFor(Guid.NewGuid(), Guid.Empty, "not-an-access");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain(["Access.InvalidAccessString", "User.EmptyRoleId"]);
        AssertNoPortCalled();
    }

    /// <summary>
    /// Every invalid access string is reported, not just the first, and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_SeveralInvalidAccessStrings_ReturnsAllErrorsAndCallsNoPort()
    {
        // Arrange
        var request = RequestFor(Guid.NewGuid(), null, "not-an-access", "also-not-an-access");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Equal("Access.InvalidAccessString", "Access.InvalidAccessString");
        AssertNoPortCalled();
    }

    /// <summary>
    /// A missing user fails with <c>User.NotFound</c>; nothing is saved or projected.
    /// </summary>
    [Test]
    public async Task Handle_UserNotFound_ReturnsNotFoundAndDoesNotSaveOrProject()
    {
        // Arrange
        SetupUser(null);
        SetupRole(RoleWith());
        var request = RequestFor(Guid.NewGuid(), Guid.NewGuid(), "users:write");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("User.NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _accessProjection.DidNotReceiveWithAnyArgs().UserAccessChanged(null!, null);
    }

    /// <summary>
    /// A given role id that matches no role fails with <c>Role.NotFound</c>; nothing is saved or projected and the
    /// user is unchanged.
    /// </summary>
    [Test]
    public async Task Handle_RoleNotFound_ReturnsRoleNotFoundAndDoesNotSaveOrProject()
    {
        // Arrange
        var originalRoleId = Guid.NewGuid();
        var user = RegularUser(roleId: originalRoleId, dedicatedAccess: "bins:read");
        SetupUser(user);
        SetupRole(null);
        var request = RequestFor(user.Id, Guid.NewGuid(), "users:write");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("Role.NotFound");
        user.RoleId.Should().Be(originalRoleId);
        user.DedicatedAccess.Should().BeEquivalentTo(AccessesOf("bins:read"));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _accessProjection.DidNotReceiveWithAnyArgs().UserAccessChanged(null!, null);
    }

    /// <summary>
    /// The root user's access cannot be changed: <c>User.RootAccessIsImmutable</c> is returned and nothing is saved
    /// or projected.
    /// </summary>
    [Test]
    public async Task Handle_RootUser_ReturnsRootAccessIsImmutableAndDoesNotSaveOrProject()
    {
        // Arrange
        var root = RootUser();
        var role = RoleWith("bins:read");
        SetupUser(root);
        SetupRole(role);
        var request = RequestFor(root.Id, role.Id, "users:write");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("User.RootAccessIsImmutable");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _accessProjection.DidNotReceiveWithAnyArgs().UserAccessChanged(null!, null);
    }

    /// <summary>
    /// When saving fails, its errors are returned and the access is not projected.
    /// </summary>
    [Test]
    public async Task Handle_SaveReturnsError_ReturnsErrorAndDoesNotProject()
    {
        // Arrange
        var user = RegularUser(dedicatedAccess: "bins:read");
        SetupUser(user);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Error.Failure("Db.Failed"));
        var request = RequestFor(user.Id, null, "users:write");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("Db.Failed");
        await _accessProjection.DidNotReceiveWithAnyArgs().UserAccessChanged(null!, null);
    }

    /// <summary>
    /// When the projection returns errors, the result is a single <c>User.AccessProjectionFailed</c> failure carrying
    /// the user id in its metadata.
    /// </summary>
    [Test]
    public async Task Handle_ProjectionReturnsError_ReturnsAccessProjectionFailedWithUserId()
    {
        // Arrange
        var user = RegularUser(dedicatedAccess: "bins:read");
        SetupUser(user);
        SetupSaveSucceeds();
        _accessProjection.UserAccessChanged(Arg.Any<User>(), Arg.Any<Role?>(), Arg.Any<CancellationToken>())
            .Returns(Error.Failure("Idp.Failed"));
        var request = RequestFor(user.Id, null, "users:write");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.Single();
        error.Code.Should().Be("User.AccessProjectionFailed");
        error.Type.Should().Be(ErrorType.Failure);
        error.Metadata.Should().ContainKey(UserFeatureErrors.UserIdMetadataKey);
        error.Metadata![UserFeatureErrors.UserIdMetadataKey].Should().Be(user.Id);
    }

    /// <summary>
    /// When the projection throws, no exception escapes and the result is a single <c>User.AccessProjectionFailed</c>
    /// failure carrying the user id in its metadata.
    /// </summary>
    [Test]
    public async Task Handle_ProjectionThrows_ReturnsAccessProjectionFailedWithUserId()
    {
        // Arrange
        var user = RegularUser(dedicatedAccess: "bins:read");
        SetupUser(user);
        SetupSaveSucceeds();
        _accessProjection.UserAccessChanged(Arg.Any<User>(), Arg.Any<Role?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());
        var request = RequestFor(user.Id, null, "users:write");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.Single();
        error.Code.Should().Be("User.AccessProjectionFailed");
        error.Type.Should().Be(ErrorType.Failure);
        error.Metadata.Should().ContainKey(UserFeatureErrors.UserIdMetadataKey);
        error.Metadata![UserFeatureErrors.UserIdMetadataKey].Should().Be(user.Id);
    }
}
