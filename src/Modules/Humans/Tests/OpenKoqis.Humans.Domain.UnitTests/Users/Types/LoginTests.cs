using AwesomeAssertions;
using OpenKoqis.Humans.Domain.Users.Types;

namespace OpenKoqis.Humans.Domain.UnitTests.Users.Types;

public class LoginTests
{
    [Test]
    [Arguments("user@example.com", typeof(Email))]
    [Arguments(" User@Example.com ", typeof(Email))]
    [Arguments("+77011234567", typeof(PhoneNumber))]
    [Arguments(" +7 (701) 123-45-67 ", typeof(PhoneNumber))]
    [Arguments("ivan", typeof(Username))]
    [Arguments(" ivan-2 ", typeof(Username))]
    public void Parse_ValidInput_DetectsLoginKind(string raw, Type expected)
        => Login.Parse(raw).Value.Should().BeOfType(expected);

    [Test]
    [Arguments(" User@Example.com ", "user@example.com")]
    [Arguments(" +7 (701) 123-45-67 ", "+77011234567")]
    [Arguments(" ivan-2 ", "ivan-2")]
    public void Parse_ValidInput_StoresNormalizedValue(string raw, string expected)
        => Login.Parse(raw).Value.Value.Should().Be(expected);

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void Parse_NullOrWhiteSpace_ReturnsEmpty(string? raw)
        => Login.Parse(raw).FirstError.Code.Should().Be("Login.Empty");

    [Test]
    [Arguments("user@", "Login.InvalidEmail")]
    [Arguments("@example.com", "Login.InvalidEmail")]
    [Arguments("+7 701 CALL NOW", "Login.InvalidPhoneNumber")]
    [Arguments("+0123456789", "Login.InvalidPhoneNumber")]
    [Arguments("Ivan", "Login.InvalidUsername")]
    [Arguments("ivan ivanov", "Login.InvalidUsername")]
    [Arguments("87011234567", "Login.InvalidUsername")]
    public void Parse_InvalidInput_ReturnsErrorOfDetectedKind(string raw, string expectedCode)
    {
        var result = Login.Parse(raw);

        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(expectedCode);
    }

    [Test]
    public void Equals_SameKindAndValue_ReturnsTrue()
    {
        var left = Login.Parse("ivan").Value;
        var right = Username.Parse("ivan").Value;

        left.Should().Be(right);
        (left == right).Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Test]
    public void Equals_DifferentValue_ReturnsFalse()
        => Login.Parse("ivan").Value.Should().NotBe(Login.Parse("petr").Value);

    [Test]
    public void ToString_ReturnsValue()
        => Login.Parse(" User@Example.com ").Value.ToString().Should().Be("user@example.com");
}
