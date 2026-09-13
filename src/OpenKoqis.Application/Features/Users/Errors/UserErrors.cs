using ErrorOr;

namespace OpenKoqis.Application.Features.Users.Errors;

public static class UserErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound(
            code: "User.NotFound",
            description: $"User with ID '{id}' was not found.");

    public static Error NicknameConflict(string nickname) =>
        Error.Conflict(
            code: "User.NicknameConflict",
            description: $"User with nickname '{nickname}' already exists.");

    public static Error InvalidCredentials =>
        Error.Unauthorized(
            code: "User.InvalidCredentials",
            description: "Invalid nickname or password.");
}
