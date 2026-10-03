using AwesomeAssertions;
using OpenKoqis.Humans.Domain.Users.Errors;
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
    /// <see langword="null"/>, empty or whitespace-only input returns <see cref="LoginErrors.Empty"/>.
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
        result.FirstError.Should().Be(LoginErrors.Empty);
    }

    /// <summary>
    /// Invalid input containing '@' returns the single <see cref="LoginErrors.InvalidEmail"/>, not a generic error.
    /// </summary>
    [Test]
    [Arguments("user@")]
    [Arguments("@example.com")]
    public void Parse_InvalidEmailInput_ReturnsInvalidEmail(string raw)
    {
        // Act
        var result = Login.Parse(raw);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Should().Be(LoginErrors.InvalidEmail);
    }

    /// <summary>
    /// Invalid input starting with '+' returns the single <see cref="LoginErrors.InvalidPhoneNumber"/>,
    /// not a generic error.
    /// </summary>
    [Test]
    [Arguments("+7 701 CALL NOW")]
    [Arguments("+0123456789")]
    public void Parse_InvalidPhoneNumberInput_ReturnsInvalidPhoneNumber(string raw)
    {
        // Act
        var result = Login.Parse(raw);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Should().Be(LoginErrors.InvalidPhoneNumber);
    }

    /// <summary>
    /// Any other invalid input, including a phone number without '+', returns the single
    /// <see cref="LoginErrors.InvalidUsername"/>, not a generic error.
    /// </summary>
    [Test]
    [Arguments("Ivan")]
    [Arguments("ivan ivanov")]
    [Arguments("87011234567")]
    public void Parse_InvalidUsernameInput_ReturnsInvalidUsername(string raw)
    {
        // Act
        var result = Login.Parse(raw);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Should().Be(LoginErrors.InvalidUsername);
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
