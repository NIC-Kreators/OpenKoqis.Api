using System.Diagnostics;
using ErrorOr;
using OpenKoqis.Humans.Domain.Users.Errors;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Humans.Domain.Users.Types;

/// <summary>
/// An email address, stored trimmed and lowercased.
/// </summary>
[DebuggerDisplay("{Value}")]
public sealed class Email : Login
{
    private Email() { }

    /// <summary>
    /// Normalizes <paramref name="raw"/> and validates it as an email address.
    /// </summary>
    public static new ErrorOr<Email> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return LoginErrors.Empty;

        var normalized = raw.Trim().ToLowerInvariant();

        if (!EmailRules.IsValid(normalized))
            return LoginErrors.InvalidEmail;

        return new Email { Value = normalized };
    }
}
