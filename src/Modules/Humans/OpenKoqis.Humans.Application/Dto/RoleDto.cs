using OpenKoqis.Humans.Domain.Roles;

namespace OpenKoqis.Humans.Application.Dto;

public record RoleDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }

    /// <summary>
    /// Accesses the role bundles, written as <c>resource:permission,permission</c>.
    /// </summary>
    public required IReadOnlyList<string> Accesses { get; init; }

    /// <summary>
    /// Id of the user who created the role.
    /// </summary>
    public required Guid CreatedBy { get; init; }

    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Maps <paramref name="role"/>.
    /// </summary>
    public static RoleDto FromRole(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Accesses = [.. role.Accesses.Select(a => a.ToString())],
        CreatedBy = role.CreatedBy,
        CreatedAt = role.CreatedAt,
        UpdatedAt = role.UpdatedAt,
    };
}
