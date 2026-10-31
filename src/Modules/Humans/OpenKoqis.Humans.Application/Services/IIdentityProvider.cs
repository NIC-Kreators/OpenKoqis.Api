using ErrorOr;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Services.Errors;
using OpenKoqis.Humans.Domain.Users.Types;

namespace OpenKoqis.Humans.Application.Services;

/// <summary>
/// An account in the external identity provider.
/// </summary>
public sealed record Identity(string Id, Login Login);

/// <summary>
/// Authentication port to the external identity provider. The provider knows nothing about roles or access.
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
    /// Returns the identities whose login matches <paramref name="term"/>.
    /// </summary>
    Task<IReadOnlyList<Identity>> SearchByLogin(string term, CancellationToken ct = default);

    /// <summary>
    /// Issues tokens for the <paramref name="login"/> and <paramref name="password"/> pair. Fails with
    /// <see cref="IdentityErrors.InvalidCredentials"/> when they don't match, so it doubles as credential verification.
    /// </summary>
    Task<ErrorOr<TokensDto>> RequestTokens(Login login, string password, CancellationToken ct = default);

    /// <summary>
    /// Creates an identity with the given <paramref name="login"/> and <paramref name="password"/>.
    /// </summary>
    Task<ErrorOr<Identity>> Create(Login login, string password, CancellationToken ct = default);

    /// <summary>
    /// Sets a new <paramref name="password"/> for the identity <paramref name="identityId"/>.
    /// </summary>
    Task<ErrorOr<Success>> SetPassword(string identityId, string password, CancellationToken ct = default);

    /// <summary>
    /// Deletes the identity <paramref name="identityId"/> from the provider.
    /// </summary>
    Task<ErrorOr<Deleted>> Delete(string identityId, CancellationToken ct = default);
}
