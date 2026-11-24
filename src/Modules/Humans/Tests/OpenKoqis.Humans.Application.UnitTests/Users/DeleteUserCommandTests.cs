using AwesomeAssertions;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenKoqis.Humans.Application.Features.Users;
using OpenKoqis.Humans.Application.Features.Users.Commands;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Humans.Domain.Users;
using static OpenKoqis.Humans.Application.UnitTests.Fixtures;

namespace OpenKoqis.Humans.Application.UnitTests.Users;

public class DeleteUserCommandTests
{
    private readonly IHumansUnitOfWork _unitOfWork = Substitute.For<IHumansUnitOfWork>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IIdentityProvider _identityProvider = Substitute.For<IIdentityProvider>();

    private DeleteUserCommandHandler CreateHandler()
        => new(_unitOfWork, _userRepository, _identityProvider, NullLogger<DeleteUserCommandHandler>.Instance);

    private static DeleteUserCommandRequest RequestFor(User user) => new() { Id = user.Id };

    private void ArrangeUser(User? user, Guid id)
        => _userRepository.GetUserWithId(id, Arg.Any<CancellationToken>()).Returns(user);

    private void ArrangeIdentity(User user, bool? isEnabled)
        => _identityProvider.FindById(user.IdentityId, Arg.Any<CancellationToken>())
            .Returns(isEnabled is null ? null : IdentityOf(user.Login.Value, user.IdentityId, isEnabled.Value));

    private void ArrangeSetEnabled(string identityId, bool enabled, ErrorOr<Success> result)
        => _identityProvider.SetEnabled(identityId, enabled, Arg.Any<CancellationToken>()).Returns(result);

    private void ArrangeSave(ErrorOr<Success> result)
        => _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(result);

    /// <summary>
    /// A regular user with an enabled identity is deleted: the result is <see cref="Result.Deleted"/> and the
    /// user's <c>DeletedAt</c> is set.
    /// </summary>
    [Test]
    public async Task Handle_ExistingUserWithEnabledIdentity_ReturnsDeletedAndSetsDeletedAt()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        ArrangeSetEnabled(user.IdentityId, false, Result.Success);
        ArrangeSave(Result.Success);

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Deleted);
        user.DeletedAt.Should().NotBeNull();
    }

    /// <summary>
    /// An enabled identity is disabled in the identity provider and the change is saved once.
    /// </summary>
    [Test]
    public async Task Handle_ExistingUserWithEnabledIdentity_DisablesIdentityAndSaves()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        ArrangeSetEnabled(user.IdentityId, false, Result.Success);
        ArrangeSave(Result.Success);

        // Act
        await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        await _identityProvider.Received(1).SetEnabled(user.IdentityId, false, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// An unknown user id yields <c>User.NotFound</c> and the identity provider is not called.
    /// </summary>
    [Test]
    public async Task Handle_UserNotFound_ReturnsNotFoundWithoutCallingIdentityProvider()
    {
        // Arrange
        var id = Guid.NewGuid();
        ArrangeUser(null, id);

        // Act
        var result = await CreateHandler().Handle(new DeleteUserCommandRequest { Id = id }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("User.NotFound");
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        _identityProvider.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// The root user cannot be deleted: <c>User.RootCannotBeDeleted</c> is returned.
    /// </summary>
    [Test]
    public async Task Handle_RootUser_ReturnsRootCannotBeDeleted()
    {
        // Arrange
        var root = RootUser();
        ArrangeUser(root, root.Id);
        ArrangeIdentity(root, isEnabled: true);
        ArrangeSave(Result.Success);

        // Act
        var result = await CreateHandler().Handle(RequestFor(root), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("User.RootCannotBeDeleted");
        root.DeletedAt.Should().BeNull();
    }

    /// <summary>
    /// Deleting the root user touches neither the identity provider nor the unit of work.
    /// </summary>
    [Test]
    public async Task Handle_RootUser_DoesNotCallIdentityProviderOrSave()
    {
        // Arrange
        var root = RootUser();
        ArrangeUser(root, root.Id);
        ArrangeIdentity(root, isEnabled: true);
        ArrangeSave(Result.Success);

        // Act
        await CreateHandler().Handle(RequestFor(root), CancellationToken.None);

        // Assert
        _identityProvider.ReceivedCalls().Should().BeEmpty();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// An identity that is already disabled is not touched and the user is still deleted.
    /// </summary>
    [Test]
    public async Task Handle_IdentityAlreadyDisabled_DoesNotCallSetEnabledAndDeletes()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: false);
        ArrangeSave(Result.Success);

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        user.DeletedAt.Should().NotBeNull();
        await _identityProvider.DidNotReceive()
            .SetEnabled(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When saving fails for a user whose identity was already disabled, the identity is never enabled.
    /// </summary>
    [Test]
    public async Task Handle_IdentityAlreadyDisabledAndSaveFails_DoesNotEnableIdentity()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: false);
        ArrangeSave(Error.Failure("Db.Failed"));

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("Db.Failed");
        await _identityProvider.DidNotReceive()
            .SetEnabled(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// An identity missing in the provider does not block the deletion and no <c>SetEnabled</c> call is made.
    /// </summary>
    [Test]
    public async Task Handle_IdentityMissing_DeletesUserWithoutCallingSetEnabled()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: null);
        ArrangeSave(Result.Success);

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Deleted);
        user.DeletedAt.Should().NotBeNull();
        await _identityProvider.DidNotReceive()
            .SetEnabled(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When saving fails for a user whose identity is missing, the identity is never enabled.
    /// </summary>
    [Test]
    public async Task Handle_IdentityMissingAndSaveFails_DoesNotEnableIdentity()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: null);
        ArrangeSave(Error.Failure("Db.Failed"));

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        await _identityProvider.DidNotReceive()
            .SetEnabled(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When disabling the identity fails, its errors are returned as they are.
    /// </summary>
    [Test]
    public async Task Handle_DisableIdentityFails_ReturnsTheIdentityProviderErrors()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        ArrangeSetEnabled(user.IdentityId, false, Error.Failure("Idp.Failed"));
        ArrangeSave(Result.Success);

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("Idp.Failed");
    }

    /// <summary>
    /// When disabling the identity fails, nothing is saved.
    /// </summary>
    [Test]
    public async Task Handle_DisableIdentityFails_DoesNotSave()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        ArrangeSetEnabled(user.IdentityId, false, Error.Failure("Idp.Failed"));
        ArrangeSave(Result.Success);

        // Act
        await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When saving fails, the save errors are returned.
    /// </summary>
    [Test]
    public async Task Handle_SaveReturnsErrors_ReturnsTheSaveErrors()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        ArrangeSetEnabled(user.IdentityId, false, Result.Success);
        ArrangeSetEnabled(user.IdentityId, true, Result.Success);
        ArrangeSave(Error.Failure("Db.Failed"));

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("Db.Failed");
    }

    /// <summary>
    /// When saving fails after this call disabled the identity, the identity is enabled again.
    /// </summary>
    [Test]
    public async Task Handle_SaveReturnsErrorsAfterDisablingIdentity_EnablesIdentityAgain()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        ArrangeSetEnabled(user.IdentityId, false, Result.Success);
        ArrangeSetEnabled(user.IdentityId, true, Result.Success);
        ArrangeSave(Error.Failure("Db.Failed"));

        // Act
        await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        await _identityProvider.Received(1).SetEnabled(user.IdentityId, true, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When saving throws, the same exception propagates out of the handler.
    /// </summary>
    [Test]
    public async Task Handle_SaveThrows_RethrowsTheSameException()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        ArrangeSetEnabled(user.IdentityId, false, Result.Success);
        ArrangeSetEnabled(user.IdentityId, true, Result.Success);
        var exception = new InvalidOperationException("save failed");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);

        // Act
        InvalidOperationException? thrown = null;
        try
        {
            await CreateHandler().Handle(RequestFor(user), CancellationToken.None);
        }
        catch (InvalidOperationException ex)
        {
            thrown = ex;
        }

        // Assert
        thrown.Should().BeSameAs(exception);
    }

    /// <summary>
    /// When saving throws after this call disabled the identity, the identity is enabled again.
    /// </summary>
    [Test]
    public async Task Handle_SaveThrowsAfterDisablingIdentity_EnablesIdentityAgain()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        ArrangeSetEnabled(user.IdentityId, false, Result.Success);
        ArrangeSetEnabled(user.IdentityId, true, Result.Success);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("save failed"));

        // Act
        try
        {
            await CreateHandler().Handle(RequestFor(user), CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // The exception is the expected outcome; only the compensation is asserted.
        }

        // Assert
        await _identityProvider.Received(1).SetEnabled(user.IdentityId, true, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When saving throws for a user whose identity was already disabled, the identity is never enabled.
    /// </summary>
    [Test]
    public async Task Handle_SaveThrowsWithIdentityAlreadyDisabled_DoesNotEnableIdentity()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: false);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("save failed"));

        // Act
        try
        {
            await CreateHandler().Handle(RequestFor(user), CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // The exception is the expected outcome; only the absence of compensation is asserted.
        }

        // Assert
        await _identityProvider.DidNotReceive()
            .SetEnabled(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When re-enabling the identity returns errors, the original save error is still returned.
    /// </summary>
    [Test]
    public async Task Handle_SaveFailsAndReEnableReturnsErrors_ReturnsTheOriginalSaveError()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        ArrangeSetEnabled(user.IdentityId, false, Result.Success);
        ArrangeSetEnabled(user.IdentityId, true, Error.Failure("Idp.EnableFailed"));
        ArrangeSave(Error.Failure("Db.Failed"));

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("Db.Failed");
    }

    /// <summary>
    /// When re-enabling the identity throws, the original save error is still returned and no exception escapes.
    /// </summary>
    [Test]
    public async Task Handle_SaveFailsAndReEnableThrows_ReturnsTheOriginalSaveError()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        ArrangeSetEnabled(user.IdentityId, false, Result.Success);
        _identityProvider.SetEnabled(user.IdentityId, true, Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("idp down"));
        ArrangeSave(Error.Failure("Db.Failed"));

        // Act
        var result = await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("Db.Failed");
    }

    /// <summary>
    /// When saving throws and re-enabling also fails, the original save exception propagates.
    /// </summary>
    [Test]
    public async Task Handle_SaveThrowsAndReEnableThrows_RethrowsTheOriginalSaveException()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        ArrangeSetEnabled(user.IdentityId, false, Result.Success);
        _identityProvider.SetEnabled(user.IdentityId, true, Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("idp down"));
        var exception = new InvalidOperationException("save failed");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);

        // Act
        InvalidOperationException? thrown = null;
        try
        {
            await CreateHandler().Handle(RequestFor(user), CancellationToken.None);
        }
        catch (InvalidOperationException ex)
        {
            thrown = ex;
        }

        // Assert
        thrown.Should().BeSameAs(exception);
    }

    /// <summary>
    /// A successful deletion does not enable the identity again.
    /// </summary>
    [Test]
    public async Task Handle_SaveSucceeds_DoesNotEnableIdentity()
    {
        // Arrange
        var user = RegularUser();
        ArrangeUser(user, user.Id);
        ArrangeIdentity(user, isEnabled: true);
        ArrangeSetEnabled(user.IdentityId, false, Result.Success);
        ArrangeSave(Result.Success);

        // Act
        await CreateHandler().Handle(RequestFor(user), CancellationToken.None);

        // Assert
        await _identityProvider.DidNotReceive().SetEnabled(user.IdentityId, true, Arg.Any<CancellationToken>());
    }
}
