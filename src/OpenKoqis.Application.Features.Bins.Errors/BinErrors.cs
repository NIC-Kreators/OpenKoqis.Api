using ErrorOr;

namespace OpenKoqis.Application.Features.Bins;

public static class BinErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound(
            code: "Bin.NotFound",
            description: $"Bin with ID '{id}' was not found.");

    public static Error InvalidTelemetryTimestamp() =>
        Error.Validation(
            code: "Bin.InvalidTelemetryTimestamp",
            description: "Telemetry.LastUpdated is invalid. Provide a valid UTC timestamp or omit the field to use the server time.");
}
