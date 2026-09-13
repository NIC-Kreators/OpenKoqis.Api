using ErrorOr;

namespace OpenKoqis.Domain.Models;

public class FillLevel(int value) : Shared.ValueObject
{
    public int Value { get; } = value;

    public static ErrorOr<FillLevel> Create(int value) => value is < 0 or > 100
        ? Error.Validation("FillLevel.Invalid", "Fill level must be between 0 and 100.")
        : new FillLevel(value);

    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}
