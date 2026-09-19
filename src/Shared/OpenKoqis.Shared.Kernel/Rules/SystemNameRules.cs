namespace OpenKoqis.Shared.Kernel.Rules;

public static class SystemNameRules
{
    public static bool IsValid(ReadOnlySpan<char> name, int maxLength = 16)
    {
        if (name.Length is < 1 || name.Length > maxLength)
            return false;
        if (!char.IsAsciiLetterLower(name[0]) || name[^1] == '-')
            return false;

        foreach (var c in name)
            if (!char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '-')
                return false;

        return !name.Contains("--", StringComparison.Ordinal);
    }
}
