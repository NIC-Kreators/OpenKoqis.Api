using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Users;

namespace OpenKoqis.Humans.Application.Dto;

public record UserDto
{
    public required Guid Id { get; init; }
    public required string IdentityId { get; init; }
    public required string Login { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }

    public required string FullName { get; init; }

    /// <summary>
    /// Whether this user is the root administrator, whose role and dedicated access cannot be changed and who
    /// cannot be deleted.
    /// </summary>
    public required bool IsRoot { get; init; }

    public Guid? RoleId { get; init; }

    /// <summary>
    /// Name of the role <see cref="RoleId"/> points to; <c>null</c> when the user has no role or the role no longer
    /// exists.
    /// </summary>
    public string? RoleName { get; init; }

    /// <summary>
    /// Accesses granted to the user directly, written as <c>resource:permission,permission</c>.
    /// </summary>
    public required IReadOnlyList<string> DedicatedAccess { get; init; }

    public Guid? LocationId { get; init; }

    /// <summary>
    /// When the user was soft-deleted; <c>null</c> while the user is active.
    /// </summary>
    public DateTime? DeletedAt { get; init; }

    /// <summary>
    /// Maps <paramref name="user"/>, taking <see cref="RoleName"/> from <paramref name="role"/>.
    /// </summary>
    /// <param name="user">The user to map.</param>
    /// <param name="role">
    /// The role the user is assigned to; <c>null</c> when the user has no role or the role no longer exists.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="role"/> is not the role the user is assigned to.</exception>
    public static UserDto FromUser(User user, Role? role)
    {
        if (role is not null && role.Id != user.RoleId)
            throw new ArgumentException(
                $"Role {role.Id} is not the role user {user.Id} is assigned to ({user.RoleId?.ToString() ?? "none"}).",
                nameof(role));

        return new UserDto
        {
            Id = user.Id,
            IdentityId = user.IdentityId,
            Login = user.Login.Value,
            Email = user.Email?.Value,
            PhoneNumber = user.PhoneNumber?.Value,
            FullName = user.Name.FullName,
            IsRoot = user.IsRoot,
            RoleId = user.RoleId,
            RoleName = role?.Name,
            DedicatedAccess = [.. user.DedicatedAccess.Select(a => a.ToString())],
            LocationId = user.LocationId,
            DeletedAt = user.DeletedAt,
        };
    }
}
