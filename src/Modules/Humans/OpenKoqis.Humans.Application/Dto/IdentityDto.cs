using OpenKoqis.Humans.Application.Services;

namespace OpenKoqis.Humans.Application.Dto;

/// <summary>
/// An identity of the external identity provider, as offered for linking to a new user.
/// </summary>
/// <param name="Id">The provider-side id.</param>
/// <param name="Login">The login the identity signs in with.</param>
/// <param name="IsEnabled">Whether the identity is allowed to sign in.</param>
/// <param name="IsLinked">Whether a persisted user, soft-deleted ones included, is already linked to it.</param>
public sealed record IdentityDto(string Id, string Login, bool IsEnabled, bool IsLinked)
{
    public static IdentityDto FromIdentity(Identity identity, bool isLinked)
        => new(identity.Id, identity.Login.Value, identity.IsEnabled, isLinked);
}
