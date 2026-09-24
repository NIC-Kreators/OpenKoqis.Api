using ErrorOr;
using OpenKoqis.Humans.Domain.Errors;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Humans.Domain;

public sealed class Username : Login
{
    private Username() { }

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
