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
    public Guid? RoleId { get; init; }

    /// <summary>
    /// Accesses granted to the user directly, written as <c>resource:permission,permission</c>.
    /// </summary>
    public required IReadOnlyList<string> DedicatedAccess { get; init; }

    public Guid? LocationId { get; init; }

    public static UserDto FromUser(User user) => new()
    {
        Id = user.Id,
        IdentityId = user.IdentityId,
        Login = user.Login.Value,
        Email = user.Email?.Value,
        PhoneNumber = user.PhoneNumber?.Value,
        FullName = user.Name.FullName,
        RoleId = user.RoleId,
        DedicatedAccess = [.. user.DedicatedAccess.Select(a => a.ToString())],
        LocationId = user.LocationId,
    };
}
