using ErrorOr;

namespace OpenKoqis.Humans.Application.Features.Roles.Errors;

/// <summary>
/// Errors produced by use cases that look up roles.
/// </summary>
public static class RoleFeatureErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        code: "Role.NotFound",
        description: $"No role with id '{id}' exists.");
}
