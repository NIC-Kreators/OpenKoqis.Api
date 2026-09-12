namespace OpenKoqis.Domain.Models;

public class Username(string value) : Shared.ValueObject
{
    public string Value { get; } = string.IsNullOrWhiteSpace(value) || value.Length < 3
        ? throw new ArgumentException("Username must be at least 3 characters long.", nameof(value))
        : value;

    public static Username Parse(string value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
