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
        => IsValidSystemName(name, maxLength, allowWildcards: false);

    /// <summary>
    /// Checks that <paramref name="name"/> is kebab-case: 1 to <paramref name="maxLength"/> characters of
    /// lowercase ASCII letters, digits and single hyphens, starting with a letter and not ending with a hyphen.
    /// Allows wildcards (*) in the string.
    /// </summary>
    public static bool IsValidWildcard(ReadOnlySpan<char> name, ushort maxLength = 16)
        => IsValidSystemName(name, maxLength, allowWildcards: true);

    private static bool IsValidSystemName(ReadOnlySpan<char> name, ushort maxLength, bool allowWildcards)
    {
        if (name.Length == 0 || name.Length > maxLength)
            return false;

        if (!IsValidFirstChar(name[0], allowWildcards) ||
            name[0] == '-' ||
            name[^1] == '-' // We can't end or start the word with "-" like "-example-" - that's invalid system name.
           )
            return false;

        foreach (var c in name)
            if (!IsValidChar(c, allowWildcards))
                return false;

        return !name.Contains("--", StringComparison.Ordinal) &&
               !name.Contains("**", StringComparison.Ordinal);
    }

    private static bool IsValidFirstChar(char c, bool allowWildcards)
        => char.IsAsciiLetterLower(c) || IsAllowedWildcard(c, allowWildcards);

    private static bool IsValidChar(char c, bool allowWildcards)
        => char.IsAsciiLetterLower(c) ||
           char.IsAsciiDigit(c) ||
           c == '-' ||
           IsAllowedWildcard(c, allowWildcards);

    private static bool IsAllowedWildcard(char c, bool allowWildcards)
        => allowWildcards && c == '*';
}
