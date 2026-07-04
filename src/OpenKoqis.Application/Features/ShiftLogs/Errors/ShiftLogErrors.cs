using ErrorOr;

namespace OpenKoqis.Application.Features.ShiftLogs;

public static class ShiftLogErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound(
            code: "ShiftLog.NotFound",
            description: $"Shift log with ID '{id}' was not found.");

    public static Error UserNotFound(string userId) =>
        Error.NotFound(
            code: "ShiftLog.UserNotFound",
            description: $"User with ID '{userId}' does not exist.");
}
