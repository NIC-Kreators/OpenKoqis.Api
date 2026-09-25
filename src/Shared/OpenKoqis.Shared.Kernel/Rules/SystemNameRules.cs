namespace OpenKoqis.Shared.Kernel.Rules;

/// <summary>
/// Validation of machine-facing identifiers such as resource and permission names.
/// </summary>
public static class SystemNameRules
{
    /// <summary>
    /// Checks that <paramref name="name"/> is kebab-case: 1 to <paramref name="maxLength"/> characters of
    /// lowercase ASCII letters, digits and single hyphens, starting with a letter and not ending with a hyphen.
    /// </summary>
    public static bool IsValid(ReadOnlySpan<char> name, ushort maxLength = 16)
    {
        if (name.Length < 1 || name.Length > maxLength)
            return false;
        if (!char.IsAsciiLetterLower(name[0]) || name[^1] == '-')
            return false;

        foreach (var c in name)
            if (!char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '-')
                return false;

        return !name.Contains("--", StringComparison.Ordinal);
    }
}
