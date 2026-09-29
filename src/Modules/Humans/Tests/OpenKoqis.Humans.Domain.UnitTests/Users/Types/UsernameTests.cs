using AwesomeAssertions;
using OpenKoqis.Humans.Domain.Users.Types;

namespace OpenKoqis.Humans.Domain.UnitTests.Users.Types;

public class UsernameTests
{
    [Test]
    [Arguments("ivan", "ivan")]
    [Arguments("  ivan-ivanov2  ", "ivan-ivanov2")]
    public void Parse_ValidUsername_StoresTrimmedValue(string raw, string expected)
        => Username.Parse(raw).Value.Value.Should().Be(expected);

    [Test]
    public void Parse_UsernameOf32Characters_Succeeds()
    {
        var raw = new string('a', 32);

        Username.Parse(raw).Value.Value.Should().Be(raw);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void Parse_NullOrWhiteSpace_ReturnsEmpty(string? raw)
        => Username.Parse(raw).FirstError.Code.Should().Be("Login.Empty");

    [Test]
    [Arguments("Ivan")]
    [Arguments("1ivan")]
    [Arguments("ivan-")]
    [Arguments("ivan--ivanov")]
    [Arguments("ivan_ivanov")]
    [Arguments("ivan ivanov")]
    [Arguments("иван")]
    [Arguments("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void Parse_InvalidUsername_ReturnsInvalidUsername(string raw)
        => Username.Parse(raw).FirstError.Code.Should().Be("Login.InvalidUsername");
}
