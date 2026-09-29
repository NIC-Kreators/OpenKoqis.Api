using AwesomeAssertions;
using OpenKoqis.Humans.Domain.Users.Types;

namespace OpenKoqis.Humans.Domain.UnitTests.Users.Types;

public class PhoneNumberTests
{
    [Test]
    [Arguments("+77011234567", "+77011234567")]
    [Arguments("  +7 (701) 123-45-67  ", "+77011234567")]
    [Arguments("+1.202.555.0142", "+12025550142")]
    public void Parse_ValidNumber_StoresE164Value(string raw, string expected)
        => PhoneNumber.Parse(raw).Value.Value.Should().Be(expected);

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void Parse_NullOrWhiteSpace_ReturnsEmpty(string? raw)
        => PhoneNumber.Parse(raw).FirstError.Code.Should().Be("Login.Empty");

    [Test]
    [Arguments("87011234567")]
    [Arguments("+0 701 123 45 67")]
    [Arguments("+1234567")]
    [Arguments("+7 701 CALL NOW")]
    public void Parse_InvalidNumber_ReturnsInvalidPhoneNumber(string raw)
        => PhoneNumber.Parse(raw).FirstError.Code.Should().Be("Login.InvalidPhoneNumber");

    [Test]
    public void Equals_SameNumberWithDifferentSeparators_ReturnsTrue()
        => PhoneNumber.Parse("+7 (701) 123-45-67").Value.Should().Be(PhoneNumber.Parse("+7.701.123.45.67").Value);
}
