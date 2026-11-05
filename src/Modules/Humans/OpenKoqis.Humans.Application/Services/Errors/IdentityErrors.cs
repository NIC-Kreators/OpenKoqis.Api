using ErrorOr;
using OpenKoqis.Humans.Domain.Users.Types;

namespace OpenKoqis.Humans.Application.Services.Errors;

/// <summary>
/// Errors produced by <see cref="IIdentityProvider"/> and the use cases around it.
/// </summary>
public static class IdentityErrors
{
    public static Error NotFound(Login login) => Error.NotFound(
        code: "Identity.NotFound",
        description: $"No identity with login '{login.Value}' exists in the identity provider.");

    public static Error NotFoundById(string identityId) => Error.NotFound(
        code: "Identity.NotFoundById",
        description: $"No identity with id '{identityId}' exists in the identity provider.");

    public static Error AlreadyExists(Login login) => Error.Conflict(
        code: "Identity.AlreadyExists",
        description: $"An identity with login '{login.Value}' already exists in the identity provider.");

    public static Error InvalidCredentials => Error.Unauthorized(
        code: "Identity.InvalidCredentials",
        description: "The login or password is incorrect.");

    public static Error InvalidRefreshToken => Error.Unauthorized(
        code: "Identity.InvalidRefreshToken",
        description: "The refresh token is expired, revoked or unknown.");

    public static Error EmptyPassword => Error.Validation(
        code: "Identity.EmptyPassword",
        description: "Password cannot be empty.");
}
