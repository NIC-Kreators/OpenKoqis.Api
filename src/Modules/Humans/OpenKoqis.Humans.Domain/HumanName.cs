using System.Diagnostics;
using ErrorOr;
using OpenKoqis.Humans.Domain.Errors;
using OpenKoqis.Shared.Kernel;
using OpenKoqis.Shared.Kernel.Rules;

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
            var trimmed = word.Trim();

            if (NameRules.IsValid(trimmed))
                return HumanNameErrors.AllSymbolsShouldBeALetter;

            words[i] = trimmed;
        }

        return new HumanName
        {
            FirstName = words[0],
            LastName = words.Length >= 2 ? words[1] : null,
            MiddleName = words.Length >= 3 ? words[2] : null,
        };
    }

    protected override IEnumerable<object> GetEqualityComponents() => [FullName];
}
