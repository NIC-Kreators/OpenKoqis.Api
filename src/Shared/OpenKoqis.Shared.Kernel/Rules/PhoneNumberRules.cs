namespace OpenKoqis.Shared.Kernel.Rules;

public static class PhoneNumberRules
{
    private const string Separators = " -.()";

    /// <summary>
    /// Checks that <paramref name="e164"/> is an E.164 number: '+' followed by 8–15 digits,
    /// the first of which is not zero.
    /// </summary>
    public static bool IsValid(ReadOnlySpan<char> e164)
    {
        if (e164.Length is < 9 or > 16 || e164[0] != '+' || e164[1] == '0')
            return false;

        foreach (var c in e164[1..])
            if (!char.IsAsciiDigit(c))
                return false;

        return true;
    }

    /// <summary>
    /// Strips common separators (space, '-', '.', '(', ')') from <paramref name="raw"/>
    /// and returns whether the result is a valid E.164 number.
    /// The country code is required; no default region is assumed.
    /// </summary>
    public static bool TryNormalize(ReadOnlySpan<char> raw, out string e164)
    {
        e164 = string.Empty;

        if (raw.Length > 64)
            return false;

        Span<char> buffer = stackalloc char[raw.Length];
        var length = 0;

        foreach (var c in raw)
            if (!Separators.Contains(c))
                buffer[length++] = c;

        var normalized = buffer[..length];
        if (!IsValid(normalized))
            return false;

        e164 = normalized.ToString();
        return true;
    }
}
