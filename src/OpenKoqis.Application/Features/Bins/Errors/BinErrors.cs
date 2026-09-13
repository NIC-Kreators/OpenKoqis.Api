using ErrorOr;

namespace OpenKoqis.Application.Features.Bins.Errors;

public static class BinErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound(
            code: "Bin.NotFound",
            description: $"Bin with ID '{id}' was not found.");
}
