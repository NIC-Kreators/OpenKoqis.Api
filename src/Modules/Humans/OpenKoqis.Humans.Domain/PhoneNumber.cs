using System.Diagnostics;
using ErrorOr;
using OpenKoqis.Humans.Domain.Errors;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Humans.Domain;

/// <summary>
/// A phone number stored in E.164 format, e.g. <c>+77011234567</c>.
/// </summary>
[DebuggerDisplay("{Value}")]
public sealed class PhoneNumber : Login
{
    private PhoneNumber() { }

    /// <summary>
    /// Validates <paramref name="raw"/> as an international phone number and normalizes it to E.164.
    /// </summary>
    public static new ErrorOr<PhoneNumber> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return LoginErrors.Empty;

        if (!PhoneNumberRules.TryNormalize(raw.Trim(), out var e164))
            return LoginErrors.InvalidPhoneNumber;

        return new PhoneNumber { Value = e164 };
    }
}
