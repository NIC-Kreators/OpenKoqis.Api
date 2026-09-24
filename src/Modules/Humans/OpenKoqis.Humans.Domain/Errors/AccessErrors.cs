using ErrorOr;

namespace OpenKoqis.Humans.Domain.Errors;

/// <summary>
/// Errors produced while parsing an <see cref="Access"/>.
/// </summary>
public static class AccessErrors
{
    public static Error InvalidAccessString(ReadOnlySpan<char> received) =>
        Error.Validation(
            code: "Access.InvalidAccessString",
            description: $"AccessString provided with invalid format. Correct example: example-resource:example-resource,example-resource. Received: {received}");

    public static Error InvalidResource(ReadOnlySpan<char> received) =>
        Error.Validation(
            code: "Access.InvalidResource",
            description: $"Resource provided with invalid format. Correct example: example-resource. Received: {received}");

    public static Error InvalidPermission(ReadOnlySpan<char> received) =>
        Error.Validation(
            code: "Access.InvalidPermission",
            description: $"Permission provided with invalid format. Correct example: example-permission. Received: {received}");
}
