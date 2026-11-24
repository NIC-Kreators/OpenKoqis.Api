using ErrorOr;

namespace OpenKoqis.Humans.Application.Features.Users.Errors;

/// <summary>
/// Errors produced by the user management use cases.
/// </summary>
public static class UserFeatureErrors
{
    public const string UserIdMetadataKey = "UserId";

    public static Error NotFound(Guid id) => Error.NotFound(
        code: "User.NotFound",
        description: $"No user with id '{id}' exists.");

    /// <summary>
    /// The user change was saved, but its access could not be pushed to the identity provider. The user's tokens
    /// don't carry the change yet; repeating the access update re-projects it. The user id is in the metadata
    /// under <see cref="UserIdMetadataKey"/>.
    /// </summary>
    public static Error AccessProjectionFailed(Guid userId) => Error.Failure(
        code: "User.AccessProjectionFailed",
        description: $"User '{userId}' was saved, but its access was not applied to the identity provider. Repeat the access update to apply it.",
        metadata: new Dictionary<string, object> { [UserIdMetadataKey] = userId });

    /// <summary>
    /// A password, blank or not, was given for a login whose identity already exists.
    /// </summary>
    public static Error PasswordNotAllowedForExistingIdentity => Error.Validation(
        code: "User.PasswordNotAllowedForExistingIdentity",
        description: "A password can only be set when a new identity is created; this login already has one.");
}
