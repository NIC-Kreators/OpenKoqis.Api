using ErrorOr;

namespace OpenKoqis.Humans.Domain.Roles.Errors;

/// <summary>
/// Errors produced by <see cref="Role"/> invariants.
/// </summary>
public static class RoleErrors
{
    public static Error InvalidName => Error.Validation(
        code: "Role.InvalidName",
        description: "Name should consist with letters and be not larger 16 symbols.");

    public static Error AtLeastOneAccessShouldBeDefined => Error.Validation(
        code: "Role.AtLeastOneAccessShouldBeDefined",
        description: "You cannot create a role without any access.");

    public static Error CouldntDetermineCreator => Error.Validation(
        code: "Role.CouldntDetermineCreator",
        description: "Creator cannot be empty.");

    /// <summary>
    /// Another persisted role already has the same name, compared case-insensitively.
    /// </summary>
    public static Error NameAlreadyExists(string name) => Error.Conflict(
        code: "Role.NameAlreadyExists",
        description: $"A role named '{name}' already exists.");
}
