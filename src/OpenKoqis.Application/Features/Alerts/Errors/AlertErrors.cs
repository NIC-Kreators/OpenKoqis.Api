using ErrorOr;

namespace OpenKoqis.Application.Features.Alerts;

public static class AlertErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound(
            code: "Alert.NotFound",
            description: $"Alert with ID '{id}' was not found.");

    public static Error AlreadyResolved(string id) =>
        Error.Conflict(
            code: "Alert.AlreadyResolved",
            description: $"Alert with ID '{id}' is already resolved.");
}
