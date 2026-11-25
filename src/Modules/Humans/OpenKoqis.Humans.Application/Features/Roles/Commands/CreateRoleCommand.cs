using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Roles.Errors;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Humans.Domain.Roles.Errors;

#pragma warning disable CS9113 // Handle is not implemented yet, so the injected dependencies are unread.

namespace OpenKoqis.Humans.Application.Features.Roles.Commands;

public class CreateRoleCommandRequest : ICommand<ErrorOr<CreateRoleCommandResponse>>
{
    public required string Name { get; init; }

    /// <summary>
    /// Accesses the role bundles, each written as <c>resource:permission,permission</c>.
    /// </summary>
    public required IReadOnlyList<string> Accesses { get; init; }

    /// <summary>
    /// Id of the user creating the role.
    /// </summary>
    public required Guid CreatedBy { get; init; }
}

public record CreateRoleCommandResponse
{
    public required RoleDto Role { get; init; }
}

/// <summary>
/// Creates a role. Its accesses are pushed through <see cref="IAccessProjection"/> before it is saved; when the
/// projection fails, nothing is saved and <see cref="RoleFeatureErrors.AccessProjectionFailed"/> is returned. When the
/// save fails, for example with <see cref="RoleErrors.NameAlreadyExists"/>, the projection is removed again.
/// </summary>
public class CreateRoleCommandHandler(
    IHumansUnitOfWork unitOfWork,
    IRoleRepository roleRepository,
    IAccessProjection accessProjection,
    ILogger<CreateRoleCommandHandler> logger) : ICommandHandler<CreateRoleCommandRequest, ErrorOr<CreateRoleCommandResponse>>
{
    public ValueTask<ErrorOr<CreateRoleCommandResponse>> Handle(CreateRoleCommandRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
