using AwesomeAssertions;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Shared.Kernel.UnitTests.Rules;

public class SystemNameRulesTests
{
    /// <summary>
    /// Kebab-case names of 1 to 16 characters are accepted: lowercase ASCII letters, digits and single hyphens,
    /// starting with a letter.
    /// </summary>
    [Test]
    [Arguments("a")]
    [Arguments("bins")]
    [Arguments("bin2")]
    [Arguments("cleaning-log")]
    [Arguments("a-b-c")]
    [Arguments("abcdefghijklmnop")]
    public void IsValid_KebabCaseName_ReturnsTrue(string name)
    {
        // Act
        var isValid = SystemNameRules.IsValid(name);

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// A single '*' wildcard may stand for the whole name or for any part of it, including the start.
    /// </summary>
    [Test]
    [Arguments("*")]
    [Arguments("bin*")]
    [Arguments("*bin")]
    [Arguments("b*n")]
    [Arguments("bin-*")]
    [Arguments("*-log")]
    [Arguments("bin*-installment")]
    [Arguments("bin*-log*")]
    public void IsValid_NameWithWildcard_ReturnsTrue(string name)
    {
        // Act
        var isValid = SystemNameRules.IsValid(name);

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// Consecutive wildcards are rejected, as are wildcard names that break the hyphen rules.
    /// </summary>
    [Test]
    [Arguments("**")]
    [Arguments("bin**")]
    [Arguments("b**n")]
    [Arguments("-*")]
    [Arguments("*-")]
    [Arguments("*--log")]
    public void IsValid_InvalidWildcardName_ReturnsFalse(string name)
    {
        // Act
        var isValid = SystemNameRules.IsValid(name);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// Names are rejected when empty, longer than 16 characters, containing uppercase or non-ASCII letters,
    /// starting with a digit or hyphen, ending with a hyphen, containing "--", or containing '_', ' ' or '.'.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments("Bins")]
    [Arguments("binS")]
    [Arguments("1bin")]
    [Arguments("-bin")]
    [Arguments("bin-")]
    [Arguments("bin--log")]
    [Arguments("bin_log")]
    [Arguments("bin log")]
    [Arguments("bin.log")]
    [Arguments("корзина")]
    [Arguments("abcdefghijklmnopq")]
    public void IsValid_InvalidName_ReturnsFalse(string name)
    {
        // Act
        var isValid = SystemNameRules.IsValid(name);

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
        var isValid = SystemNameRules.IsValid(name, maxLength: 32);

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// A name one character over a custom <c>maxLength</c> is rejected.
    /// </summary>
    [Test]
    public void IsValid_NameOverCustomMaxLength_ReturnsFalse()
    {
        // Arrange
        var name = new string('a', 33);

        // Act
        var isValid = SystemNameRules.IsValid(name, maxLength: 32);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// A name one character over the default 16-character maximum is rejected.
    /// </summary>
    [Test]
    public void IsValid_NameOverDefaultMaxLength_ReturnsFalse()
    {
        // Arrange
        var name = new string('a', 17);

        // Act
        var isValid = SystemNameRules.IsValid(name);

        // Assert
        isValid.Should().BeFalse();
    }
}
