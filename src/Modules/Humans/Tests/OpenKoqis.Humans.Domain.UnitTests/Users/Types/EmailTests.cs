using AwesomeAssertions;
using OpenKoqis.Humans.Domain.Users.Errors;
using OpenKoqis.Humans.Domain.Users.Types;

namespace OpenKoqis.Humans.Domain.UnitTests.Users.Types;

public class EmailTests
{
    /// <summary>
    /// A valid address is stored trimmed and lowercased.
    /// </summary>
    [Test]
    [Arguments("user@example.com", "user@example.com")]
    [Arguments("  John.Doe@Example.COM  ", "john.doe@example.com")]
    public void Parse_ValidEmail_StoresTrimmedLowercasedValue(string raw, string expected)
    {
        // Act
        var result = Email.Parse(raw);

        // Assert
        result.Value.Value.Should().Be(expected);
    }

    /// <summary>
    /// <see langword="null"/>, empty or whitespace-only input returns <see cref="LoginErrors.Empty"/>.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void Parse_NullOrWhiteSpace_ReturnsEmpty(string? raw)
    {
        // Act
        var result = Email.Parse(raw);

        // Assert
        result.FirstError.Should().Be(LoginErrors.Empty);
    }

    /// <summary>
    /// Input that fails the email rules returns <see cref="LoginErrors.InvalidEmail"/>.
    /// </summary>
    [Test]
    [Arguments("ivan")]
    [Arguments("user@example")]
    [Arguments("user@@example.com")]
    [Arguments("us er@example.com")]
    public void Parse_InvalidEmail_ReturnsInvalidEmail(string raw)
    {
        // Act
        var result = Email.Parse(raw);

        // Assert
        result.FirstError.Should().Be(LoginErrors.InvalidEmail);
    }

    /// <summary>
    /// Addresses that differ only in case are equal, since both are lowercased on parse.
    /// </summary>
    [Test]
    public void Equals_SameAddressInDifferentCase_ReturnsTrue()
    {
        // Act
        var left = Email.Parse("User@Example.com").Value;
        var right = Email.Parse("user@example.COM").Value;

        // Assert
        left.Should().Be(right);
    }
}
