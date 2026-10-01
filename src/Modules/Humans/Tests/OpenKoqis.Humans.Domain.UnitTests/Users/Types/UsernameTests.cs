using AwesomeAssertions;
using OpenKoqis.Humans.Domain.Users.Types;

namespace OpenKoqis.Humans.Domain.UnitTests.Users.Types;

public class UsernameTests
{
    /// <summary>
    /// A valid kebab-case username is stored trimmed.
    /// </summary>
    [Test]
    [Arguments("ivan", "ivan")]
    [Arguments("  ivan-ivanov2  ", "ivan-ivanov2")]
    public void Parse_ValidUsername_StoresTrimmedValue(string raw, string expected)
    {
        // Act
        var result = Username.Parse(raw);

        // Assert
        result.Value.Value.Should().Be(expected);
    }

    /// <summary>
    /// A username of exactly 32 characters, the maximum, is accepted.
    /// </summary>
    [Test]
    public void Parse_UsernameOf32Characters_Succeeds()
    {
        // Arrange
        var raw = new string('a', 32);

        // Act
        var result = Username.Parse(raw);

        // Assert
        result.Value.Value.Should().Be(raw);
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
        var result = Username.Parse(raw);

        // Assert
        result.FirstError.Code.Should().Be("Login.Empty");
    }

    /// <summary>
    /// Input that is not lowercase kebab-case of at most 32 characters returns <c>Login.InvalidUsername</c>;
    /// uppercase letters are rejected rather than lowercased.
    /// </summary>
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
    {
        // Act
        var result = Username.Parse(raw);

        // Assert
        result.FirstError.Code.Should().Be("Login.InvalidUsername");
    }
}
