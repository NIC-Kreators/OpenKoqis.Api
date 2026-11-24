using AwesomeAssertions;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenKoqis.Humans.Application.Features.Roles;
using OpenKoqis.Humans.Application.Features.Users;
using OpenKoqis.Humans.Application.Features.Users.Commands;
using OpenKoqis.Humans.Application.Features.Users.Errors;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Humans.Application.Services.Errors;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Humans.Domain.Users.Errors;
using static OpenKoqis.Humans.Application.UnitTests.Fixtures;

namespace OpenKoqis.Humans.Application.UnitTests.Users;

public class RestoreUserCommandTests
{
    private readonly IHumansUnitOfWork _unitOfWork = Substitute.For<IHumansUnitOfWork>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IIdentityProvider _identityProvider = Substitute.For<IIdentityProvider>();
    private readonly IAccessProjection _accessProjection = Substitute.For<IAccessProjection>();

    private RestoreUserCommandHandler CreateHandler() => new(
        _unitOfWork,
        _userRepository,
        _roleRepository,
        _identityProvider,
        _accessProjection,
        NullLogger<RestoreUserCommandHandler>.Instance);

    private static RestoreUserCommandRequest RequestFor(User user) => new() { Id = user.Id };

    private static User DeletedUser(Guid? roleId = null)
    {
        var user = RegularUser(roleId: roleId);
        user.Delete();
        return user;
    }

    private void ArrangeUser(User? user, Guid id)
        => _userRepository.GetUserWithIdIncludingDeleted(id, Arg.Any<CancellationToken>()).Returns(user);

    private void ArrangeRole(Role? role)
        => _roleRepository.GetRoleWithId(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(role);

    private void ArrangeIdentity(User user, bool? isEnabled)
        => _identityProvider.FindById(user.IdentityId, Arg.Any<CancellationToken>())
            .Returns(isEnabled is null ? null : IdentityOf(user.Login.Value, user.IdentityId, isEnabled.Value));

    private void ArrangeSetEnabled(string identityId, bool enabled, ErrorOr<Success> result)
        => _identityProvider.SetEnabled(identityId, enabled, Arg.Any<CancellationToken>()).Returns(result);

    private void ArrangeSave(ErrorOr<Success> result)
        => _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(result);

    private void ArrangeProjection(ErrorOr<Success> result)
        => _accessProjection.UserAccessChanged(Arg.Any<User>(), Arg.Any<Role?>(), Arg.Any<CancellationToken>())
            .Returns(result);

    /// <summary>
    /// A deleted user whose identity is disabled, with a successful enable, save and projection.
    /// </summary>
    private User ArrangeHappyPath(Guid? roleId = null, Role? role = null)
    {
        var user = DeletedUser(roleId);
        ArrangeUser(user, user.Id);
        ArrangeRole(role);
        ArrangeIdentity(user, isEnabled: false);
        ArrangeSetEnabled(user.IdentityId, true, Result.Success);
        ArrangeSetEnabled(user.IdentityId, false, Result.Success);
        ArrangeSave(Result.Success);
        ArrangeProjection(Result.Success);
        return user;
    }

    private async Task<Exception?> CaptureExceptionAsync(User user, CancellationToken ct = default)
    {
        try
        {
            await CreateHandler().Handle(RequestFor(user), ct);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// An unknown user id fails with <c>User.NotFound</c>, the user is loaded including soft-deleted ones, and neither
    /// the identity provider nor the unit of work is called.
    /// </summary>
    [Test]
    public async Task Handle_UserNotFound_ReturnsNotFoundWithoutCallingIdentityProviderOrSave()
    {
        // Arrange
        var id = Guid.NewGuid();
        ArrangeUser(null, id);

        // Act
        var result = await CreateHandler().Handle(new RestoreUserCommandRequest { Id = id }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be(UserFeatureErrors.NotFound(id).Code);
        await _userRepository.Received(1).GetUserWithIdIncludingDeleted(id, Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().GetUserWithId(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _identityProvider.ReceivedCalls().Should().BeEmpty();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A user that is not deleted fails with <c>User.NotDeleted</c>; the identity provider, the unit of work and the
    /// projection are not called.
    /// </summary>
    [Test]
    public async Task Handle_UserNotDeleted_ReturnsNotDeletedAndCallsNothingElse()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: false);
        ArrangeSave(Result.Success);
        ArrangeProjection(Result.Success);

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be(UserErrors.NotDeleted.Code);
        _identityProvider.ReceivedCalls().Should().BeEmpty();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _accessProjection.DidNotReceiveWithAnyArgs().UserAccessChanged(null!, null);
    }

    /// <summary>
    /// A deleted user with a disabled identity gets the identity enabled once and is saved once; the result is a
    /// success, the response carries no <c>DeletedAt</c> and the user's <c>DeletedAt</c> is cleared.
    /// </summary>
    [Test]
    public async Task Handle_DeletedUserWithDisabledIdentity_EnablesIdentitySavesAndReturnsRestoredUser()
    {
        // Arrange
        var user = ArrangeHappyPath();

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.User.Id.Should().Be(user.Id);
        result.Value.User.DeletedAt.Should().BeNull();
        user.DeletedAt.Should().BeNull();
        await _identityProvider.Received(1).SetEnabled(user.IdentityId, true, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A deleted user whose identity is already enabled is restored without touching the identity.
    /// </summary>
    [Test]
    public async Task Handle_IdentityAlreadyEnabled_DoesNotCallSetEnabledAndRestores()
    {
        // Arrange
        var user = DeletedUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        ArrangeSave(Result.Success);
        ArrangeProjection(Result.Success);

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        user.DeletedAt.Should().BeNull();
        await _identityProvider.DidNotReceive()
            .SetEnabled(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// An identity missing in the provider fails with <c>Identity.NotFoundById</c>; nothing is saved or projected.
    /// </summary>
    [Test]
    public async Task Handle_IdentityMissing_ReturnsIdentityNotFoundAndDoesNotSaveOrProject()
    {
        // Arrange
        var user = DeletedUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: null);
        ArrangeSave(Result.Success);
        ArrangeProjection(Result.Success);

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle()
            .Which.Should().Be(IdentityErrors.NotFoundById(user.IdentityId).Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _accessProjection.DidNotReceiveWithAnyArgs().UserAccessChanged(null!, null);
    }

    /// <summary>
    /// When enabling the identity fails, its errors are returned, nothing is saved and no compensation call disables
    /// the identity.
    /// </summary>
    [Test]
    public async Task Handle_EnableIdentityFails_ReturnsIdentityProviderErrorsWithoutSavingOrCompensating()
    {
        // Arrange
        var user = DeletedUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: false);
        ArrangeSetEnabled(user.IdentityId, true, Error.Failure("Idp.Failed"));
        ArrangeSave(Result.Success);
        ArrangeProjection(Result.Success);

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("Idp.Failed");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _identityProvider.DidNotReceive().SetEnabled(user.IdentityId, false, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When saving returns errors after this call enabled the identity, the save errors are returned and the identity
    /// is disabled again with a non-cancellable token.
    /// </summary>
    [Test]
    public async Task Handle_SaveReturnsErrorsAfterEnablingIdentity_ReturnsErrorsAndDisablesIdentityAgain()
    {
        // Arrange
        var user = ArrangeHappyPath();
        ArrangeSave(Error.Failure("Db.Failed"));

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("Db.Failed");
        await _identityProvider.Received(1).SetEnabled(user.IdentityId, false, CancellationToken.None);
        await _accessProjection.DidNotReceiveWithAnyArgs().UserAccessChanged(null!, null);
    }

    /// <summary>
    /// When saving throws after this call enabled the identity, the same exception propagates and the identity is
    /// disabled again with a non-cancellable token.
    /// </summary>
    [Test]
    public async Task Handle_SaveThrowsAfterEnablingIdentity_RethrowsAndDisablesIdentityAgain()
    {
        // Arrange
        var user = ArrangeHappyPath();
        var exception = new InvalidOperationException("save failed");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);

        // Act
        var thrown = await CaptureExceptionAsync(user);

        // Assert
        thrown.Should().BeSameAs(exception);
        await _identityProvider.Received(1).SetEnabled(user.IdentityId, false, CancellationToken.None);
    }

    /// <summary>
    /// When saving is cancelled after this call enabled the identity, the cancellation propagates and the identity is
    /// still disabled again with a non-cancellable token.
    /// </summary>
    [Test]
    public async Task Handle_SaveCancelledAfterEnablingIdentity_PropagatesAndStillDisablesIdentityAgain()
    {
        // Arrange
        var user = ArrangeHappyPath();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var exception = new OperationCanceledException(cts.Token);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);

        // Act
        var thrown = await CaptureExceptionAsync(user, cts.Token);

        // Assert
        thrown.Should().BeSameAs(exception);
        await _identityProvider.Received(1).SetEnabled(user.IdentityId, false, CancellationToken.None);
    }

    /// <summary>
    /// When the compensating disable returns errors, the original save error is still the result.
    /// </summary>
    [Test]
    public async Task Handle_SaveFailsAndCompensationReturnsErrors_ReturnsTheOriginalSaveError()
    {
        // Arrange
        var user = ArrangeHappyPath();
        ArrangeSetEnabled(user.IdentityId, false, Error.Failure("Idp.DisableFailed"));
        ArrangeSave(Error.Failure("Db.Failed"));

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("Db.Failed");
    }

    /// <summary>
    /// When the compensating disable throws, no exception escapes and the original save error is still the result.
    /// </summary>
    [Test]
    public async Task Handle_SaveFailsAndCompensationThrows_ReturnsTheOriginalSaveError()
    {
        // Arrange
        var user = ArrangeHappyPath();
        _identityProvider.SetEnabled(user.IdentityId, false, Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("idp down"));
        ArrangeSave(Error.Failure("Db.Failed"));

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("Db.Failed");
    }

    /// <summary>
    /// When saving throws and the compensating disable also throws, the original save exception propagates.
    /// </summary>
    [Test]
    public async Task Handle_SaveThrowsAndCompensationThrows_RethrowsTheOriginalSaveException()
    {
        // Arrange
        var user = ArrangeHappyPath();
        _identityProvider.SetEnabled(user.IdentityId, false, Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("idp down"));
        var exception = new InvalidOperationException("save failed");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);

        // Act
        var thrown = await CaptureExceptionAsync(user);

        // Assert
        thrown.Should().BeSameAs(exception);
    }

    /// <summary>
    /// When saving fails for a user whose identity was already enabled, the identity is never touched.
    /// </summary>
    [Test]
    public async Task Handle_SaveFailsWithIdentityAlreadyEnabled_DoesNotCallSetEnabled()
    {
        // Arrange
        var user = DeletedUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        ArrangeSave(Error.Failure("Db.Failed"));

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("Db.Failed");
        await _identityProvider.DidNotReceive()
            .SetEnabled(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When saving throws for a user whose identity was already enabled, the exception propagates and the identity is
    /// never touched.
    /// </summary>
    [Test]
    public async Task Handle_SaveThrowsWithIdentityAlreadyEnabled_DoesNotCallSetEnabled()
    {
        // Arrange
        var user = DeletedUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        var exception = new InvalidOperationException("save failed");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);

        // Act
        var thrown = await CaptureExceptionAsync(user);

        // Assert
        thrown.Should().BeSameAs(exception);
        await _identityProvider.DidNotReceive()
            .SetEnabled(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A successful restore does not disable the identity again.
    /// </summary>
    [Test]
    public async Task Handle_SaveSucceeds_DoesNotDisableIdentity()
    {
        // Arrange
        var user = ArrangeHappyPath();

        // Act
        await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        await _identityProvider.DidNotReceive().SetEnabled(user.IdentityId, false, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A user whose role still exists is projected with exactly that role; the response keeps the role id and carries
    /// the role name.
    /// </summary>
    [Test]
    public async Task Handle_RoleExists_ProjectsWithRoleAndReturnsRoleNameAndId()
    {
        // Arrange
        var role = RoleWith();
        var user = ArrangeHappyPath(role.Id, role);

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.User.RoleId.Should().Be(role.Id);
        result.Value.User.RoleName.Should().Be(role.Name);
        user.RoleId.Should().Be(role.Id);
        await _accessProjection.Received(1).UserAccessChanged(user, role, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A role deleted while the user was deleted is unassigned: the user's role id is cleared, the projection
    /// receives a null role and the response has neither role id nor role name.
    /// </summary>
    [Test]
    public async Task Handle_RoleNoLongerExists_ClearsRoleAndProjectsWithNull()
    {
        // Arrange
        var user = ArrangeHappyPath(Guid.NewGuid(), role: null);

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        user.RoleId.Should().BeNull();
        result.Value.User.RoleId.Should().BeNull();
        result.Value.User.RoleName.Should().BeNull();
        await _accessProjection.Received(1).UserAccessChanged(user, null, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A user without a role does not trigger a role lookup and is projected with a null role.
    /// </summary>
    [Test]
    public async Task Handle_UserWithoutRole_DoesNotLoadRoleAndProjectsWithNull()
    {
        // Arrange
        var user = ArrangeHappyPath();

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.User.RoleName.Should().BeNull();
        await _roleRepository.DidNotReceive().GetRoleWithId(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _accessProjection.Received(1).UserAccessChanged(user, null, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When the projection returns errors, the result is <c>User.AccessProjectionFailed</c> for the user, the user was
    /// still saved once and is restored.
    /// </summary>
    [Test]
    public async Task Handle_ProjectionReturnsError_ReturnsAccessProjectionFailedAfterSaving()
    {
        // Arrange
        var user = ArrangeHappyPath();
        ArrangeProjection(Error.Failure("Idp.Failed"));

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle()
            .Which.Should().Be(UserFeatureErrors.AccessProjectionFailed(user.Id).Code);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        user.DeletedAt.Should().BeNull();
    }

    /// <summary>
    /// When the projection throws, no exception escapes and the result is <c>User.AccessProjectionFailed</c> for the
    /// user.
    /// </summary>
    [Test]
    public async Task Handle_ProjectionThrows_ReturnsAccessProjectionFailed()
    {
        // Arrange
        var user = ArrangeHappyPath();
        _accessProjection.UserAccessChanged(Arg.Any<User>(), Arg.Any<Role?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle()
            .Which.Should().Be(UserFeatureErrors.AccessProjectionFailed(user.Id).Code);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
