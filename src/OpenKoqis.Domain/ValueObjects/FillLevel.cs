namespace OpenKoqis.Domain.Models;

public class FillLevel(int value) : Shared.ValueObject
{
    public int Value { get; } = value is < 0 or > 100
        ? throw new ArgumentException("Fill level must be between 0 and 100.", nameof(value))
        : value;

    public static FillLevel Parse(int value) => new(value);

    // Allows seamless comparison like `fillLevel >= 90`
    public static implicit operator int(FillLevel fillLevel) => fillLevel.Value;

    protected override IEnumerable<object> GetEqualityComponents() => [Value];
}
