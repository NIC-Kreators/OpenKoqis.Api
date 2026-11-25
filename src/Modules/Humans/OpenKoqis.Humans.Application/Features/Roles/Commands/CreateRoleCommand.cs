using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Roles.Errors;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Roles.Errors;
using OpenKoqis.Shared.Kernel.Access;

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
    public async ValueTask<ErrorOr<CreateRoleCommandResponse>> Handle(CreateRoleCommandRequest request, CancellationToken cancellationToken)
    {
        var parsedAccesses = request.Accesses.Select(Access.Parse).ToList();

        List<Error> inputErrors = [.. parsedAccesses.SelectMany(a => a.ErrorsOrEmptyList)];

        if (request.Accesses.Count is 0)
            inputErrors.Add(RoleErrors.AtLeastOneAccessShouldBeDefined);

        if (inputErrors.Count is not 0)
            return inputErrors;

        var created = Role.Create(new Role.CreationAttributes
        {
            Name = request.Name,
            Accesses = parsedAccesses.Select(a => a.Value),
            CreatedBy = request.CreatedBy,
        });

        if (created.IsError)
            return created.Errors;

        var role = created.Value;

        if (!await TryProjectRoleAsync(role, cancellationToken))
            return RoleFeatureErrors.AccessProjectionFailed(role.Id);

        var isCommitted = false;

        try
        {
            roleRepository.Add(role);

            var saved = await unitOfWork.SaveChangesAsync(cancellationToken);

            if (saved.IsError)
                return saved.Errors;

            isCommitted = true;

            return new CreateRoleCommandResponse { Role = RoleDto.FromRole(role) };
        }
        finally
        {
            if (!isCommitted)
                await RemoveProjectionAsync(role.Id);
        }
    }

    private async Task<bool> TryProjectRoleAsync(Role role, CancellationToken cancellationToken)
    {
        try
        {
            var projected = await accessProjection.RoleChanged(role, cancellationToken);

            if (!projected.IsError)
                return true;

            logger.LogError(
                "Projecting role {RoleId} failed; the role was not saved. Errors: {Errors}",
                role.Id,
                projected.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Projecting role {RoleId} threw; the role was not saved", role.Id);
        }

        return false;
    }

    /// <summary>
    /// Removes the projection of a role that was not saved. Uses <see cref="CancellationToken.None"/> so it completes
    /// even when the request was cancelled, and never throws, so it cannot replace the original failure.
    /// </summary>
    private async Task RemoveProjectionAsync(Guid roleId)
    {
        try
        {
            var removed = await accessProjection.RoleDeleted(roleId, CancellationToken.None);

            if (removed.IsError)
                logger.LogError(
                    "Failed to remove the projection of role {RoleId} after it was not saved; it stays in the identity provider. Errors: {Errors}",
                    roleId,
                    removed.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to remove the projection of role {RoleId} after it was not saved; it stays in the identity provider",
                roleId);
        }
    }
}
