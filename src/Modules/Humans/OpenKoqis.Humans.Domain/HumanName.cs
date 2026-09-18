using System.Diagnostics;
using ErrorOr;
using OpenKoqis.Humans.Domain.Errors;
using OpenKoqis.Shared.Kernel;

namespace OpenKoqis.Humans.Domain;

[DebuggerDisplay("{FullName}")]
public class HumanName : ValueObject
{
    public required string FirstName { get; init; }
    public string? LastName { get; init; }
    public string? MiddleName { get; init; }

    public string FullName => $"{FirstName} {LastName} {MiddleName}".Trim();

    private HumanName() { }

    public static ErrorOr<HumanName> Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return HumanNameErrors.WrongAmountOfWords(0);

        var words = raw.Split(' ');

        if (words.Length is < 1 or > 3)
            return HumanNameErrors.WrongAmountOfWords(words.Length);

        foreach (var (i, word) in words.Index())
        {
            var finalWord = CheckWord(word);

            if (finalWord.IsError)
                return finalWord.Errors;

            words[i] = finalWord.Value;
        }

        return new HumanName()
        {
            FirstName = words[0],
            LastName = words.Length >= 2 ? words[1] : null,
            MiddleName = words.Length >= 3 ? words[2] : null,
        };
    }

    private static ErrorOr<string> CheckWord(string word)
    {
        word = word.Trim();

        if (word.All(char.IsLetter) && word.Length is > 1 and < 16)
            return word;

        return HumanNameErrors.AllSymbolsShouldBeALetter;
    }

    protected override IEnumerable<object> GetEqualityComponents() => [FullName];
}
