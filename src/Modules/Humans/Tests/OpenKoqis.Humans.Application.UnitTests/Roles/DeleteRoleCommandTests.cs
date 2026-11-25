using AwesomeAssertions;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenKoqis.Humans.Application.Features.Roles;
using OpenKoqis.Humans.Application.Features.Roles.Commands;
using OpenKoqis.Humans.Application.Features.Roles.Errors;
using OpenKoqis.Humans.Application.Features.Users;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Users;
using static OpenKoqis.Humans.Application.UnitTests.Fixtures;

namespace OpenKoqis.Humans.Application.UnitTests.Roles;

public class DeleteRoleCommandTests
{
    private readonly IHumansUnitOfWork _unitOfWork = Substitute.For<IHumansUnitOfWork>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IAccessProjection _accessProjection = Substitute.For<IAccessProjection>();

    private DeleteRoleCommandHandler CreateHandler() => new(
        _unitOfWork,
        _roleRepository,
        _userRepository,
        _accessProjection,
        NullLogger<DeleteRoleCommandHandler>.Instance);

    private void SetupRole(Role? role)
        => _roleRepository.GetRoleWithId(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(role);

    private void SetupAssignedUsers(params User[] users)
        => _userRepository.GetActiveUsersWithRole(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(users);

    private void SetupSaveSucceeds()
        => _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success.ToErrorOr());

    private void SetupSaveFails()
        => _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Error.Failure("Db.Failed"));

    private void SetupProjectionSucceeds()
    {
        _accessProjection.RoleDeleted(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success.ToErrorOr());
        _accessProjection.RoleChanged(Arg.Any<Role>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success.ToErrorOr());
    }

    private void SetupHappyPath(Role role, params User[] users)
    {
        SetupRole(role);
        SetupAssignedUsers(users);
        SetupProjectionSucceeds();
        SetupSaveSucceeds();
    }

    private static User UserWithRole(Role role, string login, string dedicatedAccess)
        => RegularUser(login, $"identity-{login}", role.Id, dedicatedAccess);

    private static DeleteRoleCommandRequest RequestFor(Role role, bool force = false)
        => new() { Id = role.Id, Force = force };

    private async Task AssertNothingDeletedAsync()
    {
        await _accessProjection.DidNotReceive().RoleDeleted(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _roleRepository.DidNotReceive().Remove(Arg.Any<Role>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A role that does not exist fails with <c>Role.NotFound</c>, and neither users, projection, removal nor save are
    /// touched.
    /// </summary>
    [Test]
    public async Task Handle_RoleNotFound_ReturnsNotFoundAndTouchesNothingElse()
    {
        // Arrange
        SetupRole(null);
        var request = new DeleteRoleCommandRequest { Id = Guid.NewGuid() };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().BeEquivalentTo(["Role.NotFound"]);
        await _userRepository.DidNotReceive().GetActiveUsersWithRole(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _accessProjection.DidNotReceive().RoleDeleted(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _roleRepository.DidNotReceive().Remove(Arg.Any<Role>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A role without assigned users is deleted without forcing: the projection is removed before the save, the role
    /// is removed once and the result is <c>Deleted</c>.
    /// </summary>
    [Test]
    public async Task Handle_NoAssignedUsers_RemovesProjectionThenRemovesRoleAndSaves()
    {
        // Arrange
        var role = RoleWith("bins:read");
        SetupHappyPath(role);

        // Act
        var result = await CreateHandler().Handle(RequestFor(role), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Deleted);
        _roleRepository.Received(1).Remove(role);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Received.InOrder(() =>
        {
            _accessProjection.RoleDeleted(role.Id, Arg.Any<CancellationToken>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    /// <summary>
    /// A role assigned to active users cannot be deleted without forcing: the conflict reports the role id and the
    /// number of users, the users keep the role, and nothing is projected, removed or saved.
    /// </summary>
    [Test]
    public async Task Handle_AssignedUsersWithoutForce_ReturnsHasAssignedUsersAndChangesNothing()
    {
        // Arrange
        var role = RoleWith("bins:read");
        var first = UserWithRole(role, "first", "reports:read");
        var second = UserWithRole(role, "second", "users:write");
        SetupHappyPath(role, first, second);

        // Act
        var result = await CreateHandler().Handle(RequestFor(role), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.FirstError;
        error.Code.Should().Be("Role.HasAssignedUsers");
        error.Type.Should().Be(ErrorType.Conflict);
        error.Metadata.Should().NotBeNull();
        error.Metadata![RoleFeatureErrors.UsersCountMetadataKey].Should().Be(2);
        error.Metadata[RoleFeatureErrors.RoleIdMetadataKey].Should().Be(role.Id);
        first.RoleId.Should().Be(role.Id);
        second.RoleId.Should().Be(role.Id);
        await AssertNothingDeletedAsync();
    }

    /// <summary>
    /// A forced deletion unassigns every active user while keeping their dedicated access, removes the projection
    /// before the save, removes the role and saves once.
    /// </summary>
    [Test]
    public async Task Handle_AssignedUsersWithForce_UnassignsUsersKeepsDedicatedAccessAndDeletes()
    {
        // Arrange
        var role = RoleWith("bins:read");
        var first = UserWithRole(role, "first", "reports:read");
        var second = UserWithRole(role, "second", "users:write");
        SetupHappyPath(role, first, second);

        // Act
        var result = await CreateHandler().Handle(RequestFor(role, force: true), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Deleted);
        first.RoleId.Should().BeNull();
        second.RoleId.Should().BeNull();
        first.DedicatedAccess.Should().BeEquivalentTo(AccessesOf("reports:read"));
        second.DedicatedAccess.Should().BeEquivalentTo(AccessesOf("users:write"));
        await _userRepository.Received(1).GetActiveUsersWithRole(role.Id, Arg.Any<CancellationToken>());
        _roleRepository.Received(1).Remove(role);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Received.InOrder(() =>
        {
            _accessProjection.RoleDeleted(role.Id, Arg.Any<CancellationToken>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    /// <summary>
    /// A forced deletion of a role without assigned users behaves like a regular deletion: the role is removed and
    /// saved once.
    /// </summary>
    [Test]
    public async Task Handle_ForceWithoutAssignedUsers_DeletesRoleAndSaves()
    {
        // Arrange
        var role = RoleWith("bins:read");
        SetupHappyPath(role);

        // Act
        var result = await CreateHandler().Handle(RequestFor(role, force: true), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Deleted);
        _roleRepository.Received(1).Remove(role);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When the projection of the deletion returns an error, the single <c>Role.AccessProjectionFailed</c> error is
    /// returned with the role id, the user keeps the role, and the role is neither removed nor saved.
    /// </summary>
    [Test]
    public async Task Handle_ProjectionReturnsError_ReturnsAccessProjectionFailedAndSavesNothing()
    {
        // Arrange
        var role = RoleWith("bins:read");
        var user = UserWithRole(role, "first", "reports:read");
        SetupHappyPath(role, user);
        _accessProjection.RoleDeleted(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Error.Failure("Idp.Failed"));

        // Act
        var result = await CreateHandler().Handle(RequestFor(role, force: true), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.FirstError;
        error.Code.Should().Be("Role.AccessProjectionFailed");
        error.Type.Should().Be(ErrorType.Failure);
        error.Metadata.Should().NotBeNull();
        error.Metadata![RoleFeatureErrors.RoleIdMetadataKey].Should().Be(role.Id);
        user.RoleId.Should().Be(role.Id);
        _roleRepository.DidNotReceive().Remove(Arg.Any<Role>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When the projection of the deletion throws, the exception does not escape: the single
    /// <c>Role.AccessProjectionFailed</c> error is returned, the user keeps the role, and nothing is removed or saved.
    /// </summary>
    [Test]
    public async Task Handle_ProjectionThrows_ReturnsAccessProjectionFailedAndSavesNothing()
    {
        // Arrange
        var role = RoleWith("bins:read");
        var user = UserWithRole(role, "first", "reports:read");
        SetupHappyPath(role, user);
        _accessProjection.RoleDeleted(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await CreateHandler().Handle(RequestFor(role, force: true), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.FirstError;
        error.Code.Should().Be("Role.AccessProjectionFailed");
        error.Type.Should().Be(ErrorType.Failure);
        error.Metadata.Should().NotBeNull();
        error.Metadata![RoleFeatureErrors.RoleIdMetadataKey].Should().Be(role.Id);
        user.RoleId.Should().Be(role.Id);
        _roleRepository.DidNotReceive().Remove(Arg.Any<Role>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When the save fails, its errors are returned and the projection of the role is restored.
    /// </summary>
    [Test]
    public async Task Handle_SaveFails_ReturnsSaveErrorsAndRestoresRoleProjection()
    {
        // Arrange
        var role = RoleWith("bins:read");
        SetupHappyPath(role);
        SetupSaveFails();

        // Act
        var result = await CreateHandler().Handle(RequestFor(role), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("Db.Failed");
        await _accessProjection.Received(1).RoleChanged(role, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When the save fails and restoring the projection throws, the save error is still returned and no exception
    /// escapes.
    /// </summary>
    [Test]
    public async Task Handle_SaveFailsAndCompensationThrows_ReturnsSaveErrorWithoutThrowing()
    {
        // Arrange
        var role = RoleWith("bins:read");
        SetupHappyPath(role);
        SetupSaveFails();
        _accessProjection.RoleChanged(Arg.Any<Role>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await CreateHandler().Handle(RequestFor(role), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().BeEquivalentTo(["Db.Failed"]);
    }

    /// <summary>
    /// When the save fails and restoring the projection returns an error, only the save error is returned.
    /// </summary>
    [Test]
    public async Task Handle_SaveFailsAndCompensationReturnsError_ReturnsOnlySaveError()
    {
        // Arrange
        var role = RoleWith("bins:read");
        SetupHappyPath(role);
        SetupSaveFails();
        _accessProjection.RoleChanged(Arg.Any<Role>(), Arg.Any<CancellationToken>())
            .Returns(Error.Failure("Idp.Failed"));

        // Act
        var result = await CreateHandler().Handle(RequestFor(role), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().BeEquivalentTo(["Db.Failed"]);
    }

    /// <summary>
    /// A successful forced deletion never projects individual users; only the role projection is removed.
    /// </summary>
    [Test]
    public async Task Handle_ForcedDeletionSucceeds_NeverProjectsUserAccess()
    {
        // Arrange
        var role = RoleWith("bins:read");
        var user = UserWithRole(role, "first", "reports:read");
        SetupHappyPath(role, user);

        // Act
        var result = await CreateHandler().Handle(RequestFor(role, force: true), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _accessProjection.DidNotReceive()
            .UserAccessChanged(Arg.Any<User>(), Arg.Any<Role?>(), Arg.Any<CancellationToken>());
    }
}
