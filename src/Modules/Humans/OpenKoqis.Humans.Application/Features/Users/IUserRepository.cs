using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Shared.Application.Dto;

namespace OpenKoqis.Humans.Application.Features.Users;

/// <summary>
/// Persisted users of the Humans module. Users returned by this repository are tracked: changes made to them are
/// persisted when <see cref="IHumansUnitOfWork"/> saves changes.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Returns a page of the users whose full name contains <paramref name="name"/>, case-insensitively.
    /// Soft-deleted users are excluded; users are ordered by creation, oldest first.
    /// </summary>
    Task<PaginatedResponseDto<User>> SearchUsersByName(string name, PaginatedRequestDto paginatedRequest = default, CancellationToken ct = default);

    /// <summary>
    /// Returns a page of all users. Soft-deleted users are excluded; users are ordered by creation, oldest first.
    /// </summary>
    Task<PaginatedResponseDto<User>> GetPaginatedUsers(PaginatedRequestDto pageRequest = default, CancellationToken ct = default);

    /// <summary>
    /// Returns the user with the given <paramref name="id"/>; <c>null</c> when there is none or it is soft-deleted.
    /// </summary>
    Task<User?> GetUserWithId(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Whether a root user is already persisted.
    /// </summary>
    Task<bool> IsRootAlreadyExists(CancellationToken ct = default);

    /// <summary>
    /// Whether any persisted user, soft-deleted ones included, is linked to the identity <paramref name="identityId"/>.
    /// </summary>
    Task<bool> IsIdentityLinked(string identityId, CancellationToken ct = default);

    /// <summary>
    /// Returns the subset of <paramref name="identityIds"/> that persisted users, soft-deleted ones included, are linked to.
    /// </summary>
    Task<IReadOnlySet<string>> GetLinkedIdentityIds(IReadOnlyCollection<string> identityIds, CancellationToken ct = default);

    /// <summary>
    /// Starts tracking a new user; it is persisted when <see cref="IHumansUnitOfWork"/> saves changes.
    /// </summary>
    void Add(User user);
}
