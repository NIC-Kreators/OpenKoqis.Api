using AwesomeAssertions;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Shared.Kernel.UnitTests.Rules;

public class PhoneNumberRulesTests
{
    [Test]
    [Arguments("+12345678")]
    [Arguments("+77011234567")]
    [Arguments("+12025550142")]
    [Arguments("+123456789012345")]
    public void IsValid_E164Number_ReturnsTrue(string number)
        => PhoneNumberRules.IsValid(number).Should().BeTrue();

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
        => PhoneNumberRules.IsValid(number).Should().BeFalse();

    [Test]
    [Arguments("+77011234567", "+77011234567")]
    [Arguments("+7 (701) 123-45-67", "+77011234567")]
    [Arguments("+1.202.555.0142", "+12025550142")]
    [Arguments("  +44 20 7946 0958  ", "+442079460958")]
    public void TryNormalize_NumberWithSeparators_ReturnsE164(string raw, string expected)
    {
        var result = PhoneNumberRules.TryNormalize(raw, out var e164);

        result.Should().BeTrue();
        e164.Should().Be(expected);
    }

    [Test]
    [Arguments("")]
    [Arguments("8 (701) 123-45-67")]
    [Arguments("+7/701/123/45/67")]
    [Arguments("+7 701 CALL NOW")]
    [Arguments("+0 701 123 45 67")]
    [Arguments("+1 234 567")]
    public void TryNormalize_InvalidNumber_ReturnsFalseAndEmptyString(string raw)
    {
        var result = PhoneNumberRules.TryNormalize(raw, out var e164);

        result.Should().BeFalse();
        e164.Should().BeEmpty();
    }

    [Test]
    public void TryNormalize_InputLongerThan64Characters_ReturnsFalse()
    {
        var raw = "+7" + new string(' ', 60) + "7011234567";

        var result = PhoneNumberRules.TryNormalize(raw, out var e164);

        result.Should().BeFalse();
        e164.Should().BeEmpty();
    }

    [Test]
    public void TryNormalize_InputOf64Characters_IsNormalized()
    {
        var raw = "+7" + new string(' ', 52) + "7011234567";

        var result = PhoneNumberRules.TryNormalize(raw, out var e164);

        result.Should().BeTrue();
        e164.Should().Be("+77011234567");
    }
}
