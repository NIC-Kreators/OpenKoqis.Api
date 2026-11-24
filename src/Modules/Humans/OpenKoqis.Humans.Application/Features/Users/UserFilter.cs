namespace OpenKoqis.Humans.Application.Features.Users;

/// <summary>
/// Which users <see cref="IUserReadStore.GetUsers"/> returns. Every set criterion must match; users are ordered by
/// creation, oldest first.
/// </summary>
public sealed record UserFilter
{
    /// <summary>
    /// Text the full name, login, email or phone number contains, case-insensitively; <c>null</c> matches every user.
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// The role users are assigned to; <c>null</c> matches every user.
    /// </summary>
    public Guid? RoleId { get; init; }

    /// <summary>
    /// <c>false</c> returns only active users, <c>true</c> only soft-deleted ones.
    /// </summary>
    public bool IsDeleted { get; init; }
}
