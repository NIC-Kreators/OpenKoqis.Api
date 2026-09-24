namespace OpenKoqis.Shared.Kernel.Rules;

public static class EmailRules
{
    private const string LocalPartSymbols = ".!#$%&'*+/=?^_`{|}~-";

    /// <summary>
    /// Checks a trimmed, lowercased email address against a pragmatic subset of RFC 5321.
    /// </summary>
    public static bool IsValid(ReadOnlySpan<char> email)
    {
        if (email.Length is < 3 or > 254)
            return false;

        var at = email.IndexOf('@');
        if (at < 0 || email[(at + 1)..].Contains('@'))
            return false;

        return IsValidLocalPart(email[..at]) && IsValidDomain(email[(at + 1)..]);
    }

    private static bool IsValidLocalPart(ReadOnlySpan<char> local)
    {
        if (local.Length is < 1 or > 64)
            return false;
        if (local[0] == '.' || local[^1] == '.' || local.Contains("..", StringComparison.Ordinal))
            return false;

        foreach (var c in local)
            if (!char.IsAsciiLetterOrDigit(c) && !LocalPartSymbols.Contains(c))
                return false;

        return true;
    }

    private static bool IsValidDomain(ReadOnlySpan<char> domain)
    {
        if (domain.Length is < 1 or > 253)
            return false;

        var labels = 0;
        ReadOnlySpan<char> label = default;

        foreach (var range in domain.Split('.'))
        {
            label = domain[range];
            if (!IsValidLabel(label))
                return false;
            labels++;
        }

        if (labels < 2 || label.Length < 2)
            return false;

        foreach (var c in label)
            if (!char.IsAsciiLetter(c))
                return false;

        return true;
    }

    private static bool IsValidLabel(ReadOnlySpan<char> label)
    {
        if (label.Length is < 1 or > 63)
            return false;
        if (label[0] == '-' || label[^1] == '-')
            return false;

        foreach (var c in label)
            if (!char.IsAsciiLetterOrDigit(c) && c != '-')
                return false;

        return true;
    }
}
