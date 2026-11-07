using Microsoft.Extensions.Options;
using NSubstitute;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Humans.Domain.Users.Types;
using OpenKoqis.Shared.Kernel.Access;

namespace OpenKoqis.Humans.Application.UnitTests;

/// <summary>
/// Builders for valid domain objects and port stubs, so tests only spell out what they are about.
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

    public static Identity IdentityOf(string login = "ivan", string? id = null, bool isEnabled = true)
        => new(id ?? $"identity-{Guid.NewGuid()}", LoginOf(login), isEnabled);

    /// <summary>
    /// A regular user with the <c>bins:read</c> dedicated access unless other accesses are given.
    /// </summary>
    public static User RegularUser(
        string login = "ivan",
        string identityId = "identity-1",
        Guid? roleId = null,
        params string[] dedicatedAccess)
        => User.Create(new User.CreationAttributes
        {
            Login = LoginOf(login),
            IdentityId = identityId,
            Name = HumanNameOf(),
            RoleId = roleId,
            DedicatedAccess = AccessesOf(dedicatedAccess.Length > 0 ? dedicatedAccess : ["bins:read"]),
        }).Value;

    public static User RootUser(string login = "root", string identityId = "identity-root")
        => User.CreateRoot(LoginOf(login), identityId, HumanNameOf("Root")).Value;

    public static IOptionsMonitor<HumansOptions> OptionsOf(bool isIdentityCreationAllowed = true)
    {
        var monitor = Substitute.For<IOptionsMonitor<HumansOptions>>();
        monitor.CurrentValue.Returns(new HumansOptions { IsIdentityCreationAllowed = isIdentityCreationAllowed });
        return monitor;
    }
}
