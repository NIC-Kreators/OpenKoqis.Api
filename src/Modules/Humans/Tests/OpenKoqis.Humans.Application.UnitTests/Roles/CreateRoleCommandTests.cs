using AwesomeAssertions;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenKoqis.Humans.Application.Features.Roles;
using OpenKoqis.Humans.Application.Features.Roles.Commands;
using OpenKoqis.Humans.Application.Features.Roles.Errors;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Roles.Errors;
using static OpenKoqis.Humans.Application.UnitTests.Fixtures;

namespace OpenKoqis.Humans.Application.UnitTests.Roles;

public class CreateRoleCommandTests
{
    private readonly IHumansUnitOfWork _unitOfWork = Substitute.For<IHumansUnitOfWork>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IAccessProjection _accessProjection = Substitute.For<IAccessProjection>();

    private CreateRoleCommandHandler CreateHandler() => new(
        _unitOfWork,
        _roleRepository,
        _accessProjection,
        NullLogger<CreateRoleCommandHandler>.Instance);

    private void SetupSaveSucceeds()
        => _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success.ToErrorOr());

    private void SetupSaveFails(Error error)
        => _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(error);

    private void SetupProjectionSucceeds()
    {
        _accessProjection.RoleChanged(Arg.Any<Role>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success.ToErrorOr());
        _accessProjection.RoleDeleted(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success.ToErrorOr());
    }

    private void AssertNoPortCalled()
    {
        _roleRepository.ReceivedCalls().Should().BeEmpty();
        _unitOfWork.ReceivedCalls().Should().BeEmpty();
        _accessProjection.ReceivedCalls().Should().BeEmpty();
    }

    private static CreateRoleCommandRequest RequestFor(
        string name = "Operators",
        Guid? createdBy = null,
        params string[] accesses)
        => new()
        {
            Name = name,
            Accesses = accesses.Length > 0 ? accesses : ["bins:read"],
            CreatedBy = createdBy ?? Guid.NewGuid(),
        };

    /// <summary>
    /// A valid request creates the role from the trimmed name and merged accesses, projects it before saving, adds it
    /// to the repository, saves once and returns the role in the response.
    /// </summary>
    [Test]
    public async Task Handle_ValidRequest_ProjectsAddsSavesAndReturnsRole()
    {
        // Arrange
        Role? added = null;
        _roleRepository.Add(Arg.Do<Role>(r => added = r));
        SetupProjectionSucceeds();
        SetupSaveSucceeds();
        var creator = Guid.NewGuid();
        var request = RequestFor("  Operators  ", creator, "bins:read", "bins:write", "users:read");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        added.Should().NotBeNull();
        added!.Name.Should().Be("Operators");
        added.Accesses.Should().BeEquivalentTo(AccessesOf("bins:read,write", "users:read"));
        added.CreatedBy.Should().Be(creator);
        _roleRepository.Received(1).Add(Arg.Any<Role>());
        Received.InOrder(() =>
        {
            _ = _accessProjection.RoleChanged(added, Arg.Any<CancellationToken>());
            _ = _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        result.Value.Role.Id.Should().Be(added.Id);
        result.Value.Role.Name.Should().Be("Operators");
        result.Value.Role.CreatedBy.Should().Be(creator);
        result.Value.Role.Accesses.Should().BeEquivalentTo(added.Accesses.Select(a => a.ToString()));
    }

    /// <summary>
    /// An access string that cannot be parsed fails with its access error, and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_InvalidAccessString_ReturnsAccessErrorAndCallsNoPort()
    {
        // Arrange
        var request = RequestFor(accesses: "not-an-access");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("Access.InvalidAccessString");
        AssertNoPortCalled();
    }

    /// <summary>
    /// Every invalid access string is reported, not just the first, and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_SeveralInvalidAccessStrings_ReturnsAllErrorsAndCallsNoPort()
    {
        // Arrange
        var request = RequestFor(accesses: ["not-an-access", "also-not-an-access"]);

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Equal("Access.InvalidAccessString", "Access.InvalidAccessString");
        AssertNoPortCalled();
    }

    /// <summary>
    /// An empty access list fails with <c>Role.AtLeastOneAccessShouldBeDefined</c>, and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_EmptyAccessList_ReturnsAtLeastOneAccessErrorAndCallsNoPort()
    {
        // Arrange
        var request = new CreateRoleCommandRequest { Name = "Operators", Accesses = [], CreatedBy = Guid.NewGuid() };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("Role.AtLeastOneAccessShouldBeDefined");
        AssertNoPortCalled();
    }

    /// <summary>
    /// A name that is not letters only fails with <c>Role.InvalidName</c>, and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_InvalidName_ReturnsInvalidNameAndCallsNoPort()
    {
        // Arrange
        var request = RequestFor("Op3rators");

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("Role.InvalidName");
        AssertNoPortCalled();
    }

    /// <summary>
    /// An empty creator id fails with <c>Role.CouldntDetermineCreator</c>, and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_EmptyCreatedBy_ReturnsCouldntDetermineCreatorAndCallsNoPort()
    {
        // Arrange
        var request = RequestFor(createdBy: Guid.Empty);

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("Role.CouldntDetermineCreator");
        AssertNoPortCalled();
    }

    /// <summary>
    /// An invalid name and an empty creator are reported together, and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_InvalidNameAndEmptyCreatedBy_ReturnsBothErrorsAndCallsNoPort()
    {
        // Arrange
        var request = RequestFor("Op3rators", Guid.Empty);

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain(["Role.InvalidName", "Role.CouldntDetermineCreator"]);
        AssertNoPortCalled();
    }

    /// <summary>
    /// When the projection returns errors, the result is a single <c>Role.AccessProjectionFailed</c> failure carrying
    /// the role id in its metadata; the role is neither added nor saved.
    /// </summary>
    [Test]
    public async Task Handle_ProjectionReturnsError_ReturnsAccessProjectionFailedWithRoleId()
    {
        // Arrange
        Role? projected = null;
        _accessProjection.RoleChanged(Arg.Do<Role>(r => projected = r), Arg.Any<CancellationToken>())
            .Returns(Error.Failure("Idp.Failed"));
        var request = RequestFor();

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.Single();
        error.Code.Should().Be("Role.AccessProjectionFailed");
        error.Type.Should().Be(ErrorType.Failure);
        error.Metadata.Should().ContainKey(RoleFeatureErrors.RoleIdMetadataKey);
        projected.Should().NotBeNull();
        error.Metadata![RoleFeatureErrors.RoleIdMetadataKey].Should().Be(projected!.Id);
        _roleRepository.DidNotReceive().Add(Arg.Any<Role>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When the projection throws, no exception escapes and the result is a single <c>Role.AccessProjectionFailed</c>
    /// failure carrying the role id in its metadata; the role is neither added nor saved.
    /// </summary>
    [Test]
    public async Task Handle_ProjectionThrows_ReturnsAccessProjectionFailedWithRoleId()
    {
        // Arrange
        Role? projected = null;
        _accessProjection.RoleChanged(Arg.Do<Role>(r => projected = r), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());
        var request = RequestFor();

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        var error = result.Errors.Single();
        error.Code.Should().Be("Role.AccessProjectionFailed");
        error.Type.Should().Be(ErrorType.Failure);
        error.Metadata.Should().ContainKey(RoleFeatureErrors.RoleIdMetadataKey);
        projected.Should().NotBeNull();
        error.Metadata![RoleFeatureErrors.RoleIdMetadataKey].Should().Be(projected!.Id);
        _roleRepository.DidNotReceive().Add(Arg.Any<Role>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When saving fails, its errors are returned and the projection of the added role is removed again.
    /// </summary>
    [Test]
    public async Task Handle_SaveReturnsError_ReturnsErrorAndCompensatesProjection()
    {
        // Arrange
        Role? added = null;
        _roleRepository.Add(Arg.Do<Role>(r => added = r));
        SetupProjectionSucceeds();
        SetupSaveFails(RoleErrors.NameAlreadyExists("Operators"));
        var request = RequestFor();

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("Role.NameAlreadyExists");
        added.Should().NotBeNull();
        await _accessProjection.Received(1).RoleDeleted(added!.Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When saving fails and the compensation returns an error, only the save error is returned.
    /// </summary>
    [Test]
    public async Task Handle_SaveFailsAndCompensationReturnsError_ReturnsOnlySaveError()
    {
        // Arrange
        SetupProjectionSucceeds();
        _accessProjection.RoleDeleted(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Error.Failure("Idp.Failed"));
        SetupSaveFails(RoleErrors.NameAlreadyExists("Operators"));
        var request = RequestFor();

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Equal("Role.NameAlreadyExists");
    }

    /// <summary>
    /// When saving fails and the compensation throws, no exception escapes and only the save error is returned.
    /// </summary>
    [Test]
    public async Task Handle_SaveFailsAndCompensationThrows_ReturnsOnlySaveError()
    {
        // Arrange
        SetupProjectionSucceeds();
        _accessProjection.RoleDeleted(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());
        SetupSaveFails(RoleErrors.NameAlreadyExists("Operators"));
        var request = RequestFor();

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Equal("Role.NameAlreadyExists");
    }
}
