using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Users.Types;
using OpenKoqis.Shared.Kernel.Access;

namespace OpenKoqis.Humans.Domain.UnitTests;

/// <summary>
/// Builders for valid domain objects, so tests only spell out what they are about.
/// </summary>
internal static class Fixtures
{
    public static Access AccessOf(string raw) => Access.Parse(raw).Value;

    public static Access[] AccessesOf(params string[] raw) => [.. raw.Select(AccessOf)];

    public static HumanName HumanNameOf(string raw = "Ivan Ivanov") => HumanName.Parse(raw).Value;

    public static Login LoginOf(string raw = "ivan") => Login.Parse(raw).Value;

    public static Role RoleWith(params string[] accesses) => Role.Create(new Role.CreationAttributes
    {
        Name = "Operators",
        Accesses = AccessesOf(accesses.Length > 0 ? accesses : ["bins:read"]),
        CreatedBy = Guid.NewGuid(),
    }).Value;
}
