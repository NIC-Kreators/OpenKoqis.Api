using OpenKoqis.Humans.Domain.Users;

namespace OpenKoqis.Humans.Application.Dto;

public record UserDto
{
    public required string IdentityId { get; init; }
    public required string Login { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }

    public required string FullName { get; init; }
    public Guid? RoleId { get; init; }
    public Guid? LocationId { get; init; }

    public static UserDto FromUser(User user) => new()
    {
        IdentityId = user.IdentityId,
        Login = user.Login.Value,
        Email = user.Email?.Value,
        PhoneNumber = user.PhoneNumber?.Value,
        FullName = user.Name.FullName,
        RoleId = user.RoleId,
        LocationId = user.LocationId,
    };
}
