using AwesomeAssertions;
using OpenKoqis.Humans.Domain.Users.Types;

namespace OpenKoqis.Humans.Domain.UnitTests.Users.Types;

public class EmailTests
{
    [Test]
    [Arguments("user@example.com", "user@example.com")]
    [Arguments("  John.Doe@Example.COM  ", "john.doe@example.com")]
    public void Parse_ValidEmail_StoresTrimmedLowercasedValue(string raw, string expected)
        => Email.Parse(raw).Value.Value.Should().Be(expected);

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void Parse_NullOrWhiteSpace_ReturnsEmpty(string? raw)
        => Email.Parse(raw).FirstError.Code.Should().Be("Login.Empty");

    [Test]
    [Arguments("ivan")]
    [Arguments("user@example")]
    [Arguments("user@@example.com")]
    [Arguments("us er@example.com")]
    public void Parse_InvalidEmail_ReturnsInvalidEmail(string raw)
        => Email.Parse(raw).FirstError.Code.Should().Be("Login.InvalidEmail");

    [Test]
    public void Equals_SameAddressInDifferentCase_ReturnsTrue()
        => Email.Parse("User@Example.com").Value.Should().Be(Email.Parse("user@example.COM").Value);
}
