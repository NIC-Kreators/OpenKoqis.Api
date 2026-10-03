using AwesomeAssertions;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Shared.Kernel.UnitTests.Rules;

public class NameRulesTests
{
    /// <summary>
    /// Names of 1 to 16 Unicode letters in any script and case are accepted.
    /// </summary>
    [Test]
    [Arguments("A")]
    [Arguments("Anna")]
    [Arguments("anna")]
    [Arguments("Иван")]
    [Arguments("Жанна")]
    [Arguments("José")]
    [Arguments("李")]
    [Arguments("Abcdefghijklmnop")]
    public void IsValid_LettersOnlyWithinDefaultLength_ReturnsTrue(string name)
    {
        // Act
        var isValid = NameRules.IsValid(name);

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// Names are rejected when empty, longer than 16 characters, or containing anything other than letters:
    /// spaces, hyphens, apostrophes, digits or punctuation.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments("Abcdefghijklmnopq")]
    [Arguments("Anna Maria")]
    [Arguments("Anna-Maria")]
    [Arguments("O'Brien")]
    [Arguments("Anna1")]
    [Arguments(" Anna")]
    [Arguments("Anna.")]
    public void IsValid_InvalidName_ReturnsFalse(string name)
    {
        // Act
        var isValid = NameRules.IsValid(name);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// A custom <c>maxLength</c> raises the limit: a name of exactly that length is accepted.
    /// </summary>
    [Test]
    public void IsValid_NameAtCustomMaxLength_ReturnsTrue()
    {
        // Arrange
        var name = new string('a', 32);

        // Act
        var isValid = NameRules.IsValid(name, maxLength: 32);

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// A custom <c>maxLength</c> lowers the limit: a name longer than it is rejected.
    /// </summary>
    [Test]
    public void IsValid_NameOverCustomMaxLength_ReturnsFalse()
    {
        // Act
        var isValid = NameRules.IsValid("Anna", maxLength: 3);

        // Assert
        isValid.Should().BeFalse();
    }
}
