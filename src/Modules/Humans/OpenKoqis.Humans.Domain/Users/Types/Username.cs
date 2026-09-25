using System.Diagnostics;
using ErrorOr;
using OpenKoqis.Humans.Domain.Users.Errors;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Humans.Domain.Users.Types;

/// <summary>
/// A system name chosen by the user: up to 32 lowercase latin letters, digits and single hyphens.
/// </summary>
[DebuggerDisplay("{Value}")]
public sealed class Username : Login
{
    private Username() { }

    /// <summary>
    /// Trims <paramref name="raw"/> and validates it as a username.
    /// </summary>
    public static new ErrorOr<Username> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return LoginErrors.Empty;

        var trimmed = raw.Trim();

        if (!SystemNameRules.IsValid(trimmed, maxLength: 32))
            return LoginErrors.InvalidUsername;

        return new Username { Value = trimmed };
    }
}
