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
        description: $"No identity with login '{login.Value}' exists in the identity provider, and creating identities is disabled.");

    public static Error InvalidCredentials => Error.Unauthorized(
        code: "Identity.InvalidCredentials",
        description: "The login or password is incorrect.");

    public static Error EmptyPassword => Error.Validation(
        code: "Identity.EmptyPassword",
        description: "Password cannot be empty.");
}
