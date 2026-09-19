namespace OpenKoqis.Shared.Kernel.Rules;

public static class NameRules
{
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
