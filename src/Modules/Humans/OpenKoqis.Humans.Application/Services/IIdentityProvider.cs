using ErrorOr;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Services.Errors;
using OpenKoqis.Humans.Domain.Users.Types;
using OpenKoqis.Shared.Application.Dto;

namespace OpenKoqis.Humans.Application.Services;

/// <summary>
/// An account in the external identity provider.
/// </summary>
/// <param name="Id">The provider-side id; stored on the user as <c>IdentityId</c>.</param>
/// <param name="Login">The login the identity signs in with.</param>
/// <param name="IsEnabled">Whether the identity is allowed to sign in.</param>
public sealed record Identity(string Id, Login Login, bool IsEnabled);

/// <summary>
/// Authentication port to the external identity provider: accounts, credentials and tokens.
/// It knows nothing about roles or access; those reach the tokens through <see cref="IAccessProjection"/>.
/// </summary>
public interface IIdentityProvider
{
    /// <summary>
    /// Finds the identity with the given <paramref name="login"/>; <c>null</c> when there is none.
    /// </summary>
    Task<Identity?> FindByLogin(Login login, CancellationToken ct = default);

    /// <summary>
    /// Finds the identity with the given provider-side <paramref name="identityId"/>; <c>null</c> when there is none.
    /// </summary>
    Task<Identity?> FindById(string identityId, CancellationToken ct = default);

    /// <summary>
    /// Returns a page of the identities whose login contains <paramref name="term"/>.
    /// </summary>
    Task<PaginatedResponseDto<Identity>> SearchByLogin(string term, PaginatedRequestDto pageRequest = default, CancellationToken ct = default);

    /// <summary>
    /// Issues tokens for the <paramref name="login"/> and <paramref name="password"/> pair, so it doubles as
    /// credential verification. Fails with <see cref="IdentityErrors.InvalidCredentials"/> when they don't match
    /// or the identity is disabled.
    /// </summary>
    Task<ErrorOr<TokensDto>> RequestTokens(Login login, string password, CancellationToken ct = default);

    /// <summary>
    /// Exchanges <paramref name="refreshToken"/> for a new pair of tokens. Fails with
    /// <see cref="IdentityErrors.InvalidRefreshToken"/> when it is expired, revoked or unknown.
    /// </summary>
    Task<ErrorOr<TokensDto>> RefreshTokens(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Revokes <paramref name="refreshToken"/> and the session it belongs to. Revoking a token that is already
    /// invalid succeeds.
    /// </summary>
    Task<ErrorOr<Success>> RevokeTokens(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Creates an enabled identity with the given <paramref name="login"/> and <paramref name="password"/>.
    /// Fails with <see cref="IdentityErrors.AlreadyExists"/> when the login is taken, and with validation errors
    /// when the password violates the provider's policy.
    /// </summary>
    Task<ErrorOr<Identity>> Create(Login login, string password, CancellationToken ct = default);

    /// <summary>
    /// Sets a new <paramref name="password"/> for the identity <paramref name="identityId"/>. Fails with
    /// <see cref="IdentityErrors.NotFoundById"/> when there is no such identity, and with validation errors
    /// when the password violates the provider's policy.
    /// </summary>
    Task<ErrorOr<Success>> SetPassword(string identityId, string password, CancellationToken ct = default);

    /// <summary>
    /// Allows or forbids the identity <paramref name="identityId"/> to sign in. Disabling also ends its active
    /// sessions. Fails with <see cref="IdentityErrors.NotFoundById"/> when there is no such identity.
    /// </summary>
    Task<ErrorOr<Success>> SetEnabled(string identityId, bool isEnabled, CancellationToken ct = default);

    /// <summary>
    /// Deletes the identity <paramref name="identityId"/> from the provider. Deleting an identity that no longer
    /// exists succeeds, so compensating calls can be retried.
    /// </summary>
    Task<ErrorOr<Deleted>> Delete(string identityId, CancellationToken ct = default);
}
