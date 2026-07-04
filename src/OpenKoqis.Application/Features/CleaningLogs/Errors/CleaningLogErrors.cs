using ErrorOr;

namespace OpenKoqis.Application.Features.CleaningLogs;

public static class CleaningLogErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound(
            code: "CleaningLog.NotFound",
            description: $"Cleaning log with ID '{id}' was not found.");

    public static Error BinNotFound(string binId) =>
        Error.NotFound(
            code: "CleaningLog.BinNotFound",
            description: $"Associated Bin with ID '{binId}' does not exist.");
}
