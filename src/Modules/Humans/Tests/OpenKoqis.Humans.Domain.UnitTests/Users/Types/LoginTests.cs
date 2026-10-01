using AwesomeAssertions;
using OpenKoqis.Humans.Domain.Users.Types;

namespace OpenKoqis.Humans.Domain.UnitTests.Users.Types;

public class LoginTests
{
    /// <summary>
    /// Input containing '@' parses as an <see cref="Email"/>, input starting with '+' as a <see cref="PhoneNumber"/>,
    /// and anything else as a <see cref="Username"/>; surrounding whitespace does not affect detection.
    /// </summary>
    [Test]
    [Arguments("user@example.com", typeof(Email))]
    [Arguments(" User@Example.com ", typeof(Email))]
    [Arguments("+77011234567", typeof(PhoneNumber))]
    [Arguments(" +7 (701) 123-45-67 ", typeof(PhoneNumber))]
    [Arguments("ivan", typeof(Username))]
    [Arguments(" ivan-2 ", typeof(Username))]
    public void Parse_ValidInput_DetectsLoginKind(string raw, Type expected)
    {
        // Act
        var result = Login.Parse(raw);

        // Assert
        result.Value.Should().BeOfType(expected);
    }

    /// <summary>
    /// The detected kind's normalization is applied to <see cref="Login.Value"/>.
    /// </summary>
    [Test]
    [Arguments(" User@Example.com ", "user@example.com")]
    [Arguments(" +7 (701) 123-45-67 ", "+77011234567")]
    [Arguments(" ivan-2 ", "ivan-2")]
    public void Parse_ValidInput_StoresNormalizedValue(string raw, string expected)
    {
        // Act
        var result = Login.Parse(raw);

        // Assert
        result.Value.Value.Should().Be(expected);
    }

    /// <summary>
    /// <see langword="null"/>, empty or whitespace-only input returns <c>Login.Empty</c>.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void Parse_NullOrWhiteSpace_ReturnsEmpty(string? raw)
    {
        // Act
        var result = Login.Parse(raw);

        // Assert
        result.FirstError.Code.Should().Be("Login.Empty");
    }

    /// <summary>
    /// Invalid input returns the single error of the kind it was detected as, not a generic error.
    /// </summary>
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
        // Act
        var result = Login.Parse(raw);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(expectedCode);
    }

    /// <summary>
    /// Input that both starts with '+' and contains '@' is ambiguous between a phone number and an email,
    /// so it is rejected; '@' decides the kind, so the error is <c>Login.InvalidEmail</c>.
    /// </summary>
    [Test]
    [Arguments("+user@example.com")]
    [Arguments(" +77011234567@example.com ")]
    public void Parse_LeadingPlusWithAt_ReturnsInvalidEmail(string raw)
    {
        // Act
        var result = Login.Parse(raw);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("Login.InvalidEmail");
    }

    /// <summary>
    /// A login parsed through <see cref="Login.Parse"/> equals one of the same kind and value parsed directly,
    /// and hashes the same.
    /// </summary>
    [Test]
    public void Equals_SameKindAndValue_ReturnsTrue()
    {
        // Arrange
        var left = Login.Parse("ivan").Value;
        var right = Username.Parse("ivan").Value;

        // Act
        var equalityOperator = left == right;
        var leftHash = left.GetHashCode();
        var rightHash = right.GetHashCode();

        // Assert
        left.Should().Be(right);
        equalityOperator.Should().BeTrue();
        leftHash.Should().Be(rightHash);
    }

    /// <summary>
    /// Logins of the same kind with different values are not equal.
    /// </summary>
    [Test]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var left = Login.Parse("ivan").Value;
        var right = Login.Parse("petr").Value;

        // Act
        var equals = left.Equals(right);

        // Assert
        equals.Should().BeFalse();
    }

    /// <summary>
    /// <see cref="Login.ToString"/> returns the normalized value.
    /// </summary>
    [Test]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var login = Login.Parse(" User@Example.com ").Value;

        // Act
        var text = login.ToString();

        // Assert
        text.Should().Be("user@example.com");
    }
}
