namespace OpenKoqis.Shared.Kernel.Rules;

/// <summary>
/// Validation of human names, such as a first or last name.
/// </summary>
public static class NameRules
{
    /// <summary>
    /// Checks that <paramref name="name"/> is 1 to <paramref name="maxLength"/> characters long
    /// and consists of Unicode letters only.
    /// </summary>
    public static bool IsValid(ReadOnlySpan<char> name, ushort maxLength = 16)
    {
        if (name.Length < 1 || name.Length > maxLength)
            return false;

        foreach (var symbol in name)
            if (!char.IsLetter(symbol))
                return false;

        return true;
    }
}
