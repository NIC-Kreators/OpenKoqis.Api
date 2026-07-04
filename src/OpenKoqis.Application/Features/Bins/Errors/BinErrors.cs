using ErrorOr;

namespace OpenKoqis.Application.Features.Bins;

public static class BinErrors
{
    public static Error NotFound(string id) =>
        Error.NotFound(
            code: "Bin.NotFound",
            description: $"Bin with ID '{id}' was not found.");
}
