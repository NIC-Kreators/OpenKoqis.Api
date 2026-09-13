using ErrorOr;

namespace OpenKoqis.Domain.Models;

public class Username(string value) : Shared.ValueObject
{
    public string Value { get; } = value;

    public static ErrorOr<Username> Create(string value) => string.IsNullOrWhiteSpace(value) || value.Length < 3
        ? Error.Validation("Username.Invalid", "Username must be at least 3 characters long.")
        : new Username(value);

    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}
public class FullName(string value) : Shared.ValueObject
{
    public string Value { get; } = value;

    public static ErrorOr<FullName> Create(string value) => string.IsNullOrWhiteSpace(value)
        ? Error.Validation("FullName.Required", "FullName is required.")
        : new FullName(value);

    protected override IEnumerable<object> GetEqualityComponents() => [Value];
}

public class PasswordHash(string value) : Shared.ValueObject
{
    public string Value { get; } = value;

    public static ErrorOr<PasswordHash> Create(string value) => string.IsNullOrWhiteSpace(value)
        ? Error.Validation("PasswordHash.Required", "PasswordHash is required.")
        : new PasswordHash(value);

    protected override IEnumerable<object> GetEqualityComponents() => [Value];
}
