using ErrorOr;
using OpenKoqis.Humans.Domain.Errors;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Humans.Domain;

public sealed class Email : Login
{
    private Email() { }

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
