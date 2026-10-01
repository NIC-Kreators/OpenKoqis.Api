using AwesomeAssertions;
using OpenKoqis.Humans.Domain.Users.Errors;
using OpenKoqis.Humans.Domain.Users.Types;

namespace OpenKoqis.Humans.Domain.UnitTests.Users.Types;

public class PhoneNumberTests
{
    /// <summary>
    /// A valid number is trimmed, stripped of separators and stored in E.164.
    /// </summary>
    [Test]
    [Arguments("+77011234567", "+77011234567")]
    [Arguments("  +7 (701) 123-45-67  ", "+77011234567")]
    [Arguments("+1.202.555.0142", "+12025550142")]
    public void Parse_ValidNumber_StoresE164Value(string raw, string expected)
    {
        // Act
        var result = PhoneNumber.Parse(raw);

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
        var result = PhoneNumber.Parse(raw);

        // Assert
        result.FirstError.Should().Be(LoginErrors.Empty);
    }

    /// <summary>
    /// A number without a country code, with a zero country code, too few digits or letters
    /// returns <see cref="LoginErrors.InvalidPhoneNumber"/>.
    /// </summary>
    [Test]
    [Arguments("87011234567")]
    [Arguments("+0 701 123 45 67")]
    [Arguments("+1234567")]
    [Arguments("+7 701 CALL NOW")]
    public void Parse_InvalidNumber_ReturnsInvalidPhoneNumber(string raw)
    {
        // Act
        var result = PhoneNumber.Parse(raw);

        // Assert
        result.FirstError.Should().Be(LoginErrors.InvalidPhoneNumber);
    }

    /// <summary>
    /// The same number written with different separators is equal after normalization.
    /// </summary>
    [Test]
    public void Equals_SameNumberWithDifferentSeparators_ReturnsTrue()
    {
        // Act
        var left = PhoneNumber.Parse("+7 (701) 123-45-67").Value;
        var right = PhoneNumber.Parse("+7.701.123.45.67").Value;

        // Assert
        left.Should().Be(right);
    }
}
