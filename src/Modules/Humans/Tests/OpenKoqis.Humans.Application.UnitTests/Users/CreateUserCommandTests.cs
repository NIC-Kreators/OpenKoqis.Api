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
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Humans.Domain.Users.Types;
using static OpenKoqis.Humans.Application.UnitTests.Fixtures;

namespace OpenKoqis.Humans.Application.UnitTests.Users;

public class CreateUserCommandTests
{
    private const string ValidEmail = "ivan.contact@example.com";
    private const string ValidPhone = "+14155552671";

    private readonly IHumansUnitOfWork _unitOfWork = Substitute.For<IHumansUnitOfWork>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IIdentityProvider _identityProvider = Substitute.For<IIdentityProvider>();
    private readonly IAccessProjection _accessProjection = Substitute.For<IAccessProjection>();

    private User? _added;

    public CreateUserCommandTests()
    {
        _userRepository.When(r => r.Add(Arg.Any<User>())).Do(c => _added = c.Arg<User>());
        _userRepository.IsIdentityLinked(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success.ToErrorOr());
        _accessProjection.UserAccessChanged(Arg.Any<User>(), Arg.Any<Role?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success.ToErrorOr());
        _identityProvider.Delete(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ErrorOr<Deleted>)Result.Deleted);
    }

    private CreateUserCommandHandler CreateHandler(bool isIdentityCreationAllowed = true)
        => new(
            _unitOfWork,
            _userRepository,
            _roleRepository,
            _identityProvider,
            _accessProjection,
            OptionsOf(isIdentityCreationAllowed),
            NullLogger<CreateUserCommandHandler>.Instance);

    private static CreateUserCommandRequest ValidRequest() => new()
    {
        Login = "ivan",
        FullName = "Ivan Ivanov",
        Password = "Secret#123",
        Email = ValidEmail,
        PhoneNumber = ValidPhone,
        RoleId = null,
        DedicatedAccess = ["bins:read"],
    };

    private void GivenNoIdentity()
        => _identityProvider.FindByLogin(Arg.Any<Login>(), Arg.Any<CancellationToken>()).Returns((Identity?)null);

    private Identity GivenNewIdentityCanBeCreated(string id = "identity-new")
    {
        GivenNoIdentity();
        var identity = IdentityOf("ivan", id);
        _identityProvider.Create(Arg.Any<Login>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ErrorOr<Identity>)identity);
        return identity;
    }

    private Identity GivenExistingIdentity(string id = "identity-existing")
    {
        var identity = IdentityOf("ivan", id);
        _identityProvider.FindByLogin(Arg.Any<Login>(), Arg.Any<CancellationToken>()).Returns(identity);
        return identity;
    }

    private Role GivenRole()
    {
        var role = RoleWith("bins:write");
        _roleRepository.GetRoleWithId(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        return role;
    }

    private void AssertNothingWritten()
    {
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
        _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void AssertNoIdentityProviderCalls()
        => _identityProvider.ReceivedCalls().Should().BeEmpty();

    /// <summary>
    /// A missing identity is created with the login and password, and the user is created, added, saved and projected.
    /// </summary>
    [Test]
    public async Task Handle_NewIdentity_CreatesIdentityAndUser()
    {
        // Arrange
        var identity = GivenNewIdentityCanBeCreated();
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest(), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _identityProvider.Received(1).Create(
            Arg.Is<Login>(l => l.Value == "ivan"), "Secret#123", Arg.Any<CancellationToken>());
        _added.Should().NotBeNull();
        _added!.IdentityId.Should().Be(identity.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _accessProjection.Received(1).UserAccessChanged(_added, null, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// An existing, unlinked identity is used as is: the user takes its id and no identity is created.
    /// </summary>
    [Test]
    public async Task Handle_ExistingIdentity_UsesItWithoutCreating()
    {
        // Arrange
        var identity = GivenExistingIdentity();
        var request = new CreateUserCommandRequest
        {
            Login = "ivan",
            FullName = "Ivan Ivanov",
            Password = null,
            DedicatedAccess = ["bins:read"],
        };
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        _added.Should().NotBeNull();
        _added!.IdentityId.Should().Be(identity.Id);
        await _identityProvider.DidNotReceive().Create(
            Arg.Any<Login>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Several invalid inputs are all reported together and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_SeveralInvalidInputs_ReturnsAllErrorsAndCallsNoPort()
    {
        // Arrange
        var request = new CreateUserCommandRequest
        {
            Login = "",
            FullName = "",
            Email = "not-an-email",
            PhoneNumber = "not-a-phone",
            RoleId = Guid.Empty,
            DedicatedAccess = ["bogus"],
        };
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain(
        [
            "Login.Empty",
            "HumanName.WrongAmountOfWords",
            "Login.InvalidEmail",
            "Login.InvalidPhoneNumber",
            "User.EmptyRoleId",
        ]);
        result.Errors.Should().HaveCountGreaterThan(5, "the invalid access string is reported as well");
        _roleRepository.ReceivedCalls().Should().BeEmpty();
        _identityProvider.ReceivedCalls().Should().BeEmpty();
        _userRepository.ReceivedCalls().Should().BeEmpty();
        _unitOfWork.ReceivedCalls().Should().BeEmpty();
        _accessProjection.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>
    /// An invalid login is reported as a validation error.
    /// </summary>
    [Test]
    public async Task Handle_EmptyLogin_ReturnsLoginError()
    {
        // Arrange
        var request = ValidRequest().WithLogin("");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Errors.Select(e => e.Code).Should().Contain("Login.Empty");
        AssertNoIdentityProviderCalls();
        AssertNothingWritten();
    }

    /// <summary>
    /// An invalid access string is reported by the parsing error of the access.
    /// </summary>
    [Test]
    public async Task Handle_InvalidAccessString_ReturnsAccessErrorAndCallsNoPort()
    {
        // Arrange
        var request = ValidRequest().WithAccess("bogus");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain(
            OpenKoqis.Shared.Kernel.Access.Access.Parse("bogus").FirstError.Code);
        AssertNoIdentityProviderCalls();
        AssertNothingWritten();
    }

    /// <summary>
    /// An empty dedicated access list is rejected.
    /// </summary>
    [Test]
    public async Task Handle_EmptyAccessList_ReturnsAtLeastOneAccessError()
    {
        // Arrange
        var request = ValidRequest().WithAccess();
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Errors.Select(e => e.Code).Should().Contain("User.AtLeastOneAccessShouldBeDefined");
        AssertNoIdentityProviderCalls();
        AssertNothingWritten();
    }

    /// <summary>
    /// An empty role id is rejected without any lookup.
    /// </summary>
    [Test]
    public async Task Handle_EmptyRoleId_ReturnsEmptyRoleIdError()
    {
        // Arrange
        var request = ValidRequest().WithRole(Guid.Empty);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Errors.Select(e => e.Code).Should().Contain("User.EmptyRoleId");
        await _roleRepository.DidNotReceive().GetRoleWithId(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        AssertNoIdentityProviderCalls();
        AssertNothingWritten();
    }

    /// <summary>
    /// An invalid full name is rejected.
    /// </summary>
    [Test]
    public async Task Handle_InvalidFullName_ReturnsNameError()
    {
        // Arrange
        var request = ValidRequest().WithFullName("");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.Errors.Select(e => e.Code).Should().Contain("HumanName.WrongAmountOfWords");
        AssertNoIdentityProviderCalls();
        AssertNothingWritten();
    }

    /// <summary>
    /// A role id that matches no role returns Role.NotFound and the identity provider is not touched.
    /// </summary>
    [Test]
    public async Task Handle_RoleNotFound_ReturnsRoleNotFoundWithoutIdentityProviderCall()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _roleRepository.GetRoleWithId(roleId, Arg.Any<CancellationToken>()).Returns((Role?)null);
        var request = ValidRequest().WithRole(roleId);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("Role.NotFound");
        await _roleRepository.Received(1).GetRoleWithId(roleId, Arg.Any<CancellationToken>());
        AssertNoIdentityProviderCalls();
        AssertNothingWritten();
    }

    /// <summary>
    /// An identity already linked to a user is rejected, and nothing is created, added or saved.
    /// </summary>
    [Test]
    public async Task Handle_IdentityAlreadyLinked_ReturnsIdentityAlreadyLinked()
    {
        // Arrange
        var identity = GivenExistingIdentity();
        _userRepository.IsIdentityLinked(identity.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest().WithPassword(null), CancellationToken.None);

        // Assert
        result.Errors.Select(e => e.Code).Should().Contain("User.IdentityAlreadyLinked");
        await _identityProvider.DidNotReceive().Create(
            Arg.Any<Login>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        AssertNothingWritten();
    }

    /// <summary>
    /// Any password for an existing identity is rejected, a blank one included; only <c>null</c> is accepted.
    /// </summary>
    [Test]
    [Arguments("Secret#123")]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Handle_PasswordForExistingIdentity_ReturnsPasswordNotAllowed(string password)
    {
        // Arrange
        GivenExistingIdentity();
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest().WithPassword(password), CancellationToken.None);

        // Assert
        result.Errors.Select(e => e.Code).Should().Contain("User.PasswordNotAllowedForExistingIdentity");
        await _identityProvider.DidNotReceive().Create(
            Arg.Any<Login>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        AssertNothingWritten();
    }

    /// <summary>
    /// A missing identity cannot be created when identity creation is not allowed.
    /// </summary>
    [Test]
    public async Task Handle_IdentityMissingAndCreationNotAllowed_ReturnsIdentityNotFound()
    {
        // Arrange
        GivenNoIdentity();
        var handler = CreateHandler(isIdentityCreationAllowed: false);

        // Act
        var result = await handler.Handle(ValidRequest(), CancellationToken.None);

        // Assert
        result.Errors.Select(e => e.Code).Should().Contain("Identity.NotFound");
        await _identityProvider.DidNotReceive().Create(
            Arg.Any<Login>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        AssertNothingWritten();
    }

    /// <summary>
    /// A new identity needs a non-blank password.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Handle_IdentityMissingAndBlankPassword_ReturnsEmptyPassword(string? password)
    {
        // Arrange
        GivenNoIdentity();
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest().WithPassword(password), CancellationToken.None);

        // Assert
        result.Errors.Select(e => e.Code).Should().Contain("Identity.EmptyPassword");
        await _identityProvider.DidNotReceive().Create(
            Arg.Any<Login>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        AssertNothingWritten();
    }

    /// <summary>
    /// Errors of identity creation are returned as they are and nothing is added or saved.
    /// </summary>
    [Test]
    public async Task Handle_IdentityCreateFails_ReturnsProviderErrorsAndWritesNothing()
    {
        // Arrange
        GivenNoIdentity();
        _identityProvider.Create(Arg.Any<Login>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ErrorOr<Identity>)Error.Validation("Identity.WeakPassword", "Too weak."));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest(), CancellationToken.None);

        // Assert
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Identity.WeakPassword");
        AssertNothingWritten();
    }

    /// <summary>
    /// When saving fails, the errors are returned and the identity this call created is deleted.
    /// </summary>
    [Test]
    public async Task Handle_SaveFailsAfterCreatingIdentity_ReturnsErrorsAndDeletesIdentity()
    {
        // Arrange
        var identity = GivenNewIdentityCanBeCreated();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns((ErrorOr<Success>)Error.Failure("Db.Failed"));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest(), CancellationToken.None);

        // Assert
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("Db.Failed");
        await _identityProvider.Received(1).Delete(identity.Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When saving fails but a user is linked to the created identity by then, the identity is kept.
    /// </summary>
    [Test]
    public async Task Handle_SaveFailsAndIdentityLinkedAtCompensation_KeepsIdentity()
    {
        // Arrange
        var identity = GivenNewIdentityCanBeCreated();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns((ErrorOr<Success>)Error.Failure("Db.Failed"));
        _userRepository.IsIdentityLinked(identity.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest(), CancellationToken.None);

        // Assert
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("Db.Failed");
        await _identityProvider.DidNotReceive().Delete(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When saving throws, the same exception propagates and the created identity is deleted.
    /// </summary>
    [Test]
    public async Task Handle_SaveThrowsAfterCreatingIdentity_RethrowsAndDeletesIdentity()
    {
        // Arrange
        var identity = GivenNewIdentityCanBeCreated();
        var exception = new InvalidOperationException("boom");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);
        var handler = CreateHandler();
        InvalidOperationException? thrown = null;

        // Act
        try
        {
            await handler.Handle(ValidRequest(), CancellationToken.None);
        }
        catch (InvalidOperationException ex)
        {
            thrown = ex;
        }

        // Assert
        thrown.Should().BeSameAs(exception);
        await _identityProvider.Received(1).Delete(identity.Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When saving throws and a user is linked to the created identity by then, the identity is kept.
    /// </summary>
    [Test]
    public async Task Handle_SaveThrowsAndIdentityLinkedAtCompensation_RethrowsAndKeepsIdentity()
    {
        // Arrange
        var identity = GivenNewIdentityCanBeCreated();
        var exception = new InvalidOperationException("boom");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);
        _userRepository.IsIdentityLinked(identity.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateHandler();
        InvalidOperationException? thrown = null;

        // Act
        try
        {
            await handler.Handle(ValidRequest(), CancellationToken.None);
        }
        catch (InvalidOperationException ex)
        {
            thrown = ex;
        }

        // Assert
        thrown.Should().BeSameAs(exception);
        await _identityProvider.DidNotReceive().Delete(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A failed save with an identity that already existed never deletes that identity.
    /// </summary>
    [Test]
    public async Task Handle_SaveFailsWithExistingIdentity_DoesNotDeleteIdentity()
    {
        // Arrange
        GivenExistingIdentity();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns((ErrorOr<Success>)Error.Failure("Db.Failed"));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest().WithPassword(null), CancellationToken.None);

        // Assert
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("Db.Failed");
        await _identityProvider.DidNotReceive().Delete(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A save that throws with an identity that already existed never deletes that identity.
    /// </summary>
    [Test]
    public async Task Handle_SaveThrowsWithExistingIdentity_RethrowsAndDoesNotDeleteIdentity()
    {
        // Arrange
        GivenExistingIdentity();
        var exception = new InvalidOperationException("boom");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);
        var handler = CreateHandler();
        InvalidOperationException? thrown = null;

        // Act
        try
        {
            await handler.Handle(ValidRequest().WithPassword(null), CancellationToken.None);
        }
        catch (InvalidOperationException ex)
        {
            thrown = ex;
        }

        // Assert
        thrown.Should().BeSameAs(exception);
        await _identityProvider.DidNotReceive().Delete(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A compensation that returns errors does not replace the original save errors.
    /// </summary>
    [Test]
    public async Task Handle_SaveFailsAndDeleteReturnsErrors_ReturnsOriginalSaveErrors()
    {
        // Arrange
        var identity = GivenNewIdentityCanBeCreated();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns((ErrorOr<Success>)Error.Failure("Db.Failed"));
        _identityProvider.Delete(identity.Id, Arg.Any<CancellationToken>())
            .Returns((ErrorOr<Deleted>)Error.Failure("Idp.DeleteFailed"));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest(), CancellationToken.None);

        // Assert
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("Db.Failed");
        await _identityProvider.Received(1).Delete(identity.Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A compensation that throws does not replace the original save errors.
    /// </summary>
    [Test]
    public async Task Handle_SaveFailsAndDeleteThrows_ReturnsOriginalSaveErrors()
    {
        // Arrange
        var identity = GivenNewIdentityCanBeCreated();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns((ErrorOr<Success>)Error.Failure("Db.Failed"));
        _identityProvider.Delete(identity.Id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("idp down"));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest(), CancellationToken.None);

        // Assert
        result.Errors.Select(e => e.Code).Should().ContainSingle().Which.Should().Be("Db.Failed");
        await _identityProvider.Received(1).Delete(identity.Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A compensation that throws does not replace the original save exception.
    /// </summary>
    [Test]
    public async Task Handle_SaveThrowsAndDeleteThrows_RethrowsOriginalException()
    {
        // Arrange
        var identity = GivenNewIdentityCanBeCreated();
        var exception = new InvalidOperationException("boom");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);
        _identityProvider.Delete(identity.Id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("idp down"));
        var handler = CreateHandler();
        InvalidOperationException? thrown = null;

        // Act
        try
        {
            await handler.Handle(ValidRequest(), CancellationToken.None);
        }
        catch (InvalidOperationException ex)
        {
            thrown = ex;
        }

        // Assert
        thrown.Should().BeSameAs(exception);
        await _identityProvider.Received(1).Delete(identity.Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// No access projection happens when the save fails.
    /// </summary>
    [Test]
    public async Task Handle_SaveFails_DoesNotProject()
    {
        // Arrange
        GivenNewIdentityCanBeCreated();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns((ErrorOr<Success>)Error.Failure("Db.Failed"));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest(), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        await _accessProjection.DidNotReceive().UserAccessChanged(
            Arg.Any<User>(), Arg.Any<Role?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// No access projection happens when the save throws.
    /// </summary>
    [Test]
    public async Task Handle_SaveThrows_DoesNotProject()
    {
        // Arrange
        GivenNewIdentityCanBeCreated();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));
        var handler = CreateHandler();
        var rethrown = false;

        // Act
        try
        {
            await handler.Handle(ValidRequest(), CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            rethrown = true;
        }

        // Assert
        rethrown.Should().BeTrue();
        await _accessProjection.DidNotReceive().UserAccessChanged(
            Arg.Any<User>(), Arg.Any<Role?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The projection receives the saved user and the role that was looked up.
    /// </summary>
    [Test]
    public async Task Handle_RoleGiven_ProjectsSavedUserWithRole()
    {
        // Arrange
        GivenNewIdentityCanBeCreated();
        var role = GivenRole();
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest().WithRole(role.Id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        _added.Should().NotBeNull();
        _added!.RoleId.Should().Be(role.Id);
        await _accessProjection.Received(1).UserAccessChanged(_added, role, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Without a role id the projection receives a null role.
    /// </summary>
    [Test]
    public async Task Handle_NoRoleId_ProjectsWithNullRole()
    {
        // Arrange
        GivenNewIdentityCanBeCreated();
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest(), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _accessProjection.Received(1).UserAccessChanged(
            Arg.Is<User>(u => ReferenceEquals(u, _added)), null, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A projection that returns errors yields a single AccessProjectionFailed failure carrying the user id,
    /// and the created identity is not deleted.
    /// </summary>
    [Test]
    public async Task Handle_ProjectionReturnsErrors_ReturnsAccessProjectionFailedAndKeepsIdentity()
    {
        // Arrange
        GivenNewIdentityCanBeCreated();
        _accessProjection.UserAccessChanged(Arg.Any<User>(), Arg.Any<Role?>(), Arg.Any<CancellationToken>())
            .Returns((ErrorOr<Success>)Error.Failure("Idp.Down"));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest(), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        var error = result.Errors.Should().ContainSingle().Subject;
        error.Code.Should().Be("User.AccessProjectionFailed");
        error.Type.Should().Be(ErrorType.Failure);
        _added.Should().NotBeNull();
        error.Metadata.Should().NotBeNull();
        error.Metadata![UserFeatureErrors.UserIdMetadataKey].Should().Be(_added!.Id);
        await _identityProvider.DidNotReceive().Delete(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A projection that throws yields the same AccessProjectionFailed failure, no exception escapes,
    /// and the created identity is not deleted.
    /// </summary>
    [Test]
    public async Task Handle_ProjectionThrows_ReturnsAccessProjectionFailedAndKeepsIdentity()
    {
        // Arrange
        GivenNewIdentityCanBeCreated();
        _accessProjection.UserAccessChanged(Arg.Any<User>(), Arg.Any<Role?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("idp down"));
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidRequest(), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        var error = result.Errors.Should().ContainSingle().Subject;
        error.Code.Should().Be("User.AccessProjectionFailed");
        error.Type.Should().Be(ErrorType.Failure);
        _added.Should().NotBeNull();
        error.Metadata.Should().NotBeNull();
        error.Metadata![UserFeatureErrors.UserIdMetadataKey].Should().Be(_added!.Id);
        await _identityProvider.DidNotReceive().Delete(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A successful creation returns a user DTO that reflects the created user.
    /// </summary>
    [Test]
    public async Task Handle_Success_ReturnsDtoReflectingCreatedUser()
    {
        // Arrange
        var identity = GivenNewIdentityCanBeCreated();
        var role = GivenRole();
        var request = ValidRequest().WithRole(role.Id).WithAccess("bins:read", "reports:write");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        var dto = result.Value.User;
        _added.Should().NotBeNull();
        dto.Id.Should().Be(_added!.Id);
        dto.IdentityId.Should().Be(identity.Id);
        dto.Login.Should().Be("ivan");
        dto.FullName.Should().Be("Ivan Ivanov");
        dto.Email.Should().Be(ValidEmail);
        dto.PhoneNumber.Should().Be(ValidPhone);
        dto.RoleId.Should().Be(role.Id);
        dto.DedicatedAccess.Should().BeEquivalentTo(["bins:read", "reports:write"]);
    }
}

file static class RequestExtensions
{
    public static CreateUserCommandRequest WithLogin(this CreateUserCommandRequest r, string login) => Copy(r, login: login);

    public static CreateUserCommandRequest WithFullName(this CreateUserCommandRequest r, string name) => Copy(r, fullName: name);

    public static CreateUserCommandRequest WithPassword(this CreateUserCommandRequest r, string? password)
        => Copy(r, password: password, passwordSet: true);

    public static CreateUserCommandRequest WithRole(this CreateUserCommandRequest r, Guid? roleId)
        => Copy(r, roleId: roleId, roleSet: true);

    public static CreateUserCommandRequest WithAccess(this CreateUserCommandRequest r, params string[] access)
        => Copy(r, access: access);

    private static CreateUserCommandRequest Copy(
        CreateUserCommandRequest r,
        string? login = null,
        string? fullName = null,
        string? password = null,
        bool passwordSet = false,
        Guid? roleId = null,
        bool roleSet = false,
        string[]? access = null)
        => new()
        {
            Login = login ?? r.Login,
            FullName = fullName ?? r.FullName,
            Password = passwordSet ? password : r.Password,
            Email = r.Email,
            PhoneNumber = r.PhoneNumber,
            RoleId = roleSet ? roleId : r.RoleId,
            DedicatedAccess = access ?? r.DedicatedAccess,
        };
}
