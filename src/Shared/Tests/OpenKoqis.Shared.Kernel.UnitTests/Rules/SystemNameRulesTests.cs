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
    /// <see cref="SystemNameRules.IsValid"/> rejects '*': wildcards are allowed only by
    /// <see cref="SystemNameRules.IsValidWildcard"/>.
    /// </summary>
    [Test]
    [Arguments("*")]
    [Arguments("bin*")]
    [Arguments("*bin")]
    [Arguments("b*n")]
    public void IsValid_NameWithWildcard_ReturnsFalse(string name)
    {
        // Act
        var isValid = SystemNameRules.IsValid(name);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// <see cref="SystemNameRules.IsValidWildcard"/> accepts plain kebab-case names too: the wildcard is optional.
    /// </summary>
    [Test]
    [Arguments("a")]
    [Arguments("bins")]
    [Arguments("cleaning-log")]
    [Arguments("abcdefghijklmnop")]
    public void IsValidWildcard_KebabCaseName_ReturnsTrue(string name)
    {
        // Act
        var isValid = SystemNameRules.IsValidWildcard(name);

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
    public void IsValidWildcard_NameWithWildcard_ReturnsTrue(string name)
    {
        // Act
        var isValid = SystemNameRules.IsValidWildcard(name);

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// Consecutive wildcards are rejected, and a wildcard does not exempt the rest of the name from the other rules:
    /// hyphen placement, lowercase ASCII letters only, no '_', and a letter rather than a digit first.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments("**")]
    [Arguments("bin**")]
    [Arguments("b**n")]
    [Arguments("-*")]
    [Arguments("*-")]
    [Arguments("*--log")]
    [Arguments("Bin*")]
    [Arguments("bin_*")]
    [Arguments("1*")]
    public void IsValidWildcard_InvalidName_ReturnsFalse(string name)
    {
        // Act
        var isValid = SystemNameRules.IsValidWildcard(name);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// '*' counts towards the length like any other character: a wildcard name of exactly 16 characters is accepted.
    /// </summary>
    [Test]
    public void IsValidWildcard_NameOf16Characters_ReturnsTrue()
    {
        // Act
        var isValid = SystemNameRules.IsValidWildcard("abcdefghijklmno*");

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// '*' counts towards the length like any other character: a wildcard name of 17 characters is rejected.
    /// </summary>
    [Test]
    public void IsValidWildcard_NameOf17Characters_ReturnsFalse()
    {
        // Act
        var isValid = SystemNameRules.IsValidWildcard("abcdefghijklmnop*");

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// A custom <c>maxLength</c> applies to wildcard names: a name of exactly that length is accepted
    /// and one character over it is rejected.
    /// </summary>
    [Test]
    public void IsValidWildcard_CustomMaxLength_IsEnforced()
    {
        // Arrange
        var atLimit = new string('a', 31) + "*";
        var overLimit = new string('a', 32) + "*";

        // Act
        var atLimitIsValid = SystemNameRules.IsValidWildcard(atLimit, maxLength: 32);
        var overLimitIsValid = SystemNameRules.IsValidWildcard(overLimit, maxLength: 32);

        // Assert
        atLimitIsValid.Should().BeTrue();
        overLimitIsValid.Should().BeFalse();
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
