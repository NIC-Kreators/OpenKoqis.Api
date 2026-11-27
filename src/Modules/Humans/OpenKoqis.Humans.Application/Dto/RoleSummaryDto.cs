namespace OpenKoqis.Humans.Application.Dto;

/// <summary>
/// A role as returned by role queries, together with how many users hold it.
/// </summary>
public record RoleSummaryDto : RoleDto
{
    /// <summary>
    /// Number of active users assigned to the role; soft-deleted users are not counted.
    /// </summary>
    public required int UsersCount { get; init; }
}
