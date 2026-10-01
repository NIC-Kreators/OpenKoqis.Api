using AwesomeAssertions;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Shared.Kernel.UnitTests.Rules;

public class PhoneNumberRulesTests
{
    /// <summary>
    /// '+' followed by 8 to 15 ASCII digits, the first not zero, is valid E.164.
    /// </summary>
    [Test]
    [Arguments("+12345678")]
    [Arguments("+77011234567")]
    [Arguments("+12025550142")]
    [Arguments("+123456789012345")]
    public void IsValid_E164Number_ReturnsTrue(string number)
    {
        // Act
        var isValid = PhoneNumberRules.IsValid(number);

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// Input is rejected when it has too few or too many digits, no leading '+' or a doubled one,
    /// a leading zero country code, letters, separators or non-ASCII digits.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments("+")]
    [Arguments("+1234567")]
    [Arguments("+1234567890123456")]
    [Arguments("77011234567")]
    [Arguments("++7011234567")]
    [Arguments("+07011234567")]
    [Arguments("+7701a234567")]
    [Arguments("+7 701 123 4567")]
    [Arguments("+7-701-123-4567")]
    [Arguments("+٧٧٠١١٢٣٤٥٦٧")]
    public void IsValid_NonE164Input_ReturnsFalse(string number)
    {
        // Act
        var isValid = PhoneNumberRules.IsValid(number);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// Spaces, '-', '.', '(' and ')' are stripped and the result is returned in E.164.
    /// </summary>
    [Test]
    [Arguments("+77011234567", "+77011234567")]
    [Arguments("+7 (701) 123-45-67", "+77011234567")]
    [Arguments("+1.202.555.0142", "+12025550142")]
    [Arguments("  +44 20 7946 0958  ", "+442079460958")]
    public void TryNormalize_NumberWithSeparators_ReturnsE164(string raw, string expected)
    {
        // Act
        var result = PhoneNumberRules.TryNormalize(raw, out var e164);

        // Assert
        result.Should().BeTrue();
        e164.Should().Be(expected);
    }

    /// <summary>
    /// Input that is still not E.164 after stripping separators, such as a missing country code, unsupported
    /// separators, letters, a zero country code or too few digits, fails and yields an empty string.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments("8 (701) 123-45-67")]
    [Arguments("+7/701/123/45/67")]
    [Arguments("+7 701 CALL NOW")]
    [Arguments("+0 701 123 45 67")]
    [Arguments("+1 234 567")]
    public void TryNormalize_InvalidNumber_ReturnsFalseAndEmptyString(string raw)
    {
        // Act
        var result = PhoneNumberRules.TryNormalize(raw, out var e164);

        // Assert
        result.Should().BeFalse();
        e164.Should().BeEmpty();
    }

    /// <summary>
    /// Input longer than 64 characters is rejected before stripping, even if it would normalize to a valid number.
    /// </summary>
    [Test]
    public void TryNormalize_InputLongerThan64Characters_ReturnsFalse()
    {
        // Arrange: a valid number padded with spaces to 72 characters
        var raw = "+7" + new string(' ', 60) + "7011234567";

        // Act
        var result = PhoneNumberRules.TryNormalize(raw, out var e164);

        // Assert
        result.Should().BeFalse();
        e164.Should().BeEmpty();
    }

    /// <summary>
    /// Input of exactly 64 characters, the maximum, is still normalized.
    /// </summary>
    [Test]
    public void TryNormalize_InputOf64Characters_IsNormalized()
    {
        // Arrange: a valid number padded with spaces to exactly 64 characters
        var raw = "+7" + new string(' ', 52) + "7011234567";

        // Act
        var result = PhoneNumberRules.TryNormalize(raw, out var e164);

        // Assert
        result.Should().BeTrue();
        e164.Should().Be("+77011234567");
    }
}
