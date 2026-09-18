using ErrorOr;

namespace OpenKoqis.Humans.Domain.Errors;

public static class HumanNameErrors
{
    public static Error WrongAmountOfWords(int found)
        => Error.Validation(
            code: "HumanName.WrongAmountOfWords",
            description: $"Human name should consist between 1 and 3 words. Found: {found}");

    public static Error AllSymbolsShouldBeALetter => Error.Validation(
        code: "HumanName.AllSymbolsShouldBeALetter",
        description: "Human name can be consist only with letters and each word cannot exceeds length between 1 and 16");
}
