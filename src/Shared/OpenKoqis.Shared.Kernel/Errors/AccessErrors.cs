using ErrorOr;

namespace OpenKoqis.Shared.Kernel.Errors;

/// <summary>
/// Errors produced while parsing an <see cref="Access.Access"/>.
/// </summary>
public static class AccessErrors
{
    /// <summary>
    /// The access string is not of the form <c>resource:permission,permission</c>.
    /// </summary>
    public static Error InvalidAccessString(ReadOnlySpan<char> received) =>
        Error.Validation(
            code: "Access.InvalidAccessString",
            description: $"AccessString provided with invalid format. Correct example: example-resource:example-resource,example-resource. Received: {received}");

    /// <summary>
    /// The resource name does not satisfy <see cref="Rules.SystemNameRules"/>.
    /// </summary>
    public static Error InvalidResource(ReadOnlySpan<char> received) =>
        Error.Validation(
            code: "Access.InvalidResource",
            description: $"Resource provided with invalid format. Correct example: example-resource. Received: {received}");

    /// <summary>
    /// A permission name does not satisfy <see cref="Rules.SystemNameRules"/>.
    /// </summary>
    public static Error InvalidPermission(ReadOnlySpan<char> received) =>
        Error.Validation(
            code: "Access.InvalidPermission",
            description: $"Permission provided with invalid format. Correct example: example-permission. Received: {received}");
}
