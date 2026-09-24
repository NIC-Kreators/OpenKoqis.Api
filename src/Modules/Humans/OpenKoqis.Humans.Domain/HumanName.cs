using System.Diagnostics;
using ErrorOr;
using OpenKoqis.Humans.Domain.Errors;
using OpenKoqis.Shared.Kernel;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Humans.Domain;

/// <summary>
/// A person's name of one to three words: first name, then optional last and middle names.
/// </summary>
[DebuggerDisplay("{FullName}")]
public class HumanName : ValueObject
{
    public required string FirstName { get; init; }
    public string? LastName { get; init; }
    public string? MiddleName { get; init; }

    /// <summary>
    /// The name parts joined in the order they were parsed: first, last, middle.
    /// </summary>
    public string FullName => $"{FirstName} {LastName} {MiddleName}".Trim();

    private HumanName() { }

    /// <summary>
    /// Splits <paramref name="raw"/> on spaces into first, last and middle names;
    /// every word must consist of letters only.
    /// </summary>
    public static ErrorOr<HumanName> Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return HumanNameErrors.WrongAmountOfWords(0);

        var words = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        List<Error> errors = [];

        if (words.Length is < 1 or > 3)
            errors.Add(HumanNameErrors.WrongAmountOfWords(words.Length));

        if (words.Any(w => !NameRules.IsValid(w)))
            errors.Add(HumanNameErrors.AllSymbolsShouldBeALetter);

        if (errors.Count > 0)
            return errors;

        return new HumanName
        {
            FirstName = words[0],
            LastName = words.Length >= 2 ? words[1] : null,
            MiddleName = words.Length >= 3 ? words[2] : null,
        };
    }

    protected override IEnumerable<object> GetEqualityComponents() => [FullName];
}
