using System.Diagnostics;
using ErrorOr;
using OpenKoqis.Humans.Domain.Errors;
using OpenKoqis.Shared.Kernel;

namespace OpenKoqis.Humans.Domain;

/// <summary>
/// A value a user signs in with: a <see cref="Username"/>, an <see cref="Email"/> or a <see cref="PhoneNumber"/>.
/// </summary>
[DebuggerDisplay("{Value}")]
public abstract class Login : ValueObject
{
    /// <summary>
    /// The normalized login as it is stored and compared.
    /// </summary>
    public required string Value { get; init; }

    private protected Login() { }

    /// <summary>
    /// Detects the kind of login from <paramref name="raw"/>: an '@' means an email,
    /// a leading '+' means a phone number, anything else is a username.
    /// </summary>
    public static ErrorOr<Login> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return LoginErrors.Empty;

        var trimmed = raw.Trim();

        if (trimmed.Contains('@'))
            return Email.Parse(trimmed).Then(Login (email) => email);

        if (trimmed.StartsWith('+'))
            return PhoneNumber.Parse(trimmed).Then(Login (phone) => phone);

        return Username.Parse(trimmed).Then(Login (username) => username);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object> GetEqualityComponents() => [GetType(), Value];
}
