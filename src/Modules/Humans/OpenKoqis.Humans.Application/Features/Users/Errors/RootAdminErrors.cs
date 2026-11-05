using ErrorOr;

namespace OpenKoqis.Humans.Application.Features.Users.Errors;

/// <summary>
/// Errors produced while creating the root administrator.
/// </summary>
public static class RootAdminErrors
{
    public static Error CreationInProgress => Error.Conflict(
        code: "User.RootCreationInProgress",
        description: "Root administrator creation is already in progress. Try again shortly.");
}
