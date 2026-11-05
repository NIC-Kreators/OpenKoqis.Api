using ErrorOr;
using OpenKoqis.Humans.Application.Persistence;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Shared.Application.Dto;

namespace OpenKoqis.Humans.Application.Features.Users;

public interface IUserRepository
{
    Task<PaginatedResponseDto<User>> SearchUsersByName(string name, PaginatedRequestDto paginatedRequest = default, CancellationToken ct = default);
    Task<PaginatedResponseDto<User>> GetPaginatedUsers(PaginatedRequestDto pageRequest = default, CancellationToken ct = default);
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
    /// Starts tracking a new user; it is persisted when <see cref="IHumansUnitOfWork"/> saves changes.
    /// </summary>
    void Add(User user);

    Task<ErrorOr<Updated>> UpdateUser(User user, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> DeleteUserWithId(Guid id, CancellationToken ct = default);
}
