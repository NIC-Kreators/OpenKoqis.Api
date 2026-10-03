using AwesomeAssertions;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Shared.Kernel.UnitTests.Rules;

public class EmailRulesTests
{
    /// <summary>
    /// Addresses with allowed local-part symbols, sub-domains, hyphens and digit-only labels are accepted.
    /// </summary>
    [Test]
    [Arguments("a@b.co")]
    [Arguments("john.doe@example.com")]
    [Arguments("user+tag@sub.example.org")]
    [Arguments("first_last@ex-ample.com")]
    [Arguments("o'neil@example.io")]
    [Arguments("x!#$%&'*+/=?^_`{|}~-y@example.com")]
    [Arguments("user@123.example.com")]
    public void IsValid_ValidEmail_ReturnsTrue(string email)
    {
        // Act
        var isValid = EmailRules.IsValid(email);

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// Addresses are rejected when they have no or several '@', a misplaced or doubled dot, a space or non-ASCII
    /// character, a single-label domain, a TLD that is shorter than two letters or not letters only,
    /// or a label that is empty, starts or ends with a hyphen, or contains '_'.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments("a@")]
    [Arguments("plainaddress")]
    [Arguments("@example.com")]
    [Arguments("user@")]
    [Arguments("user@@example.com")]
    [Arguments("user@exa@mple.com")]
    [Arguments(".user@example.com")]
    [Arguments("user.@example.com")]
    [Arguments("us..er@example.com")]
    [Arguments("user name@example.com")]
    [Arguments("üser@example.com")]
    [Arguments("user@example")]
    [Arguments("user@example.c")]
    [Arguments("user@example.c0m")]
    [Arguments("user@example.123")]
    [Arguments("user@.example.com")]
    [Arguments("user@example..com")]
    [Arguments("user@example.com.")]
    [Arguments("user@-example.com")]
    [Arguments("user@example-.com")]
    [Arguments("user@exa_mple.com")]
    [Arguments("user@exämple.com")]
    public void IsValid_InvalidEmail_ReturnsFalse(string email)
    {
        // Act
        var isValid = EmailRules.IsValid(email);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// A local part of exactly 64 characters, the maximum, is accepted.
    /// </summary>
    [Test]
    public void IsValid_LocalPartOf64Characters_ReturnsTrue()
    {
        // Arrange
        var email = new string('a', 64) + "@example.com";

        // Act
        var isValid = EmailRules.IsValid(email);

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// A local part one character over the 64-character maximum is rejected.
    /// </summary>
    [Test]
    public void IsValid_LocalPartOf65Characters_ReturnsFalse()
    {
        // Arrange
        var email = new string('a', 65) + "@example.com";

        // Act
        var isValid = EmailRules.IsValid(email);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// A domain label of exactly 63 characters, the maximum, is accepted.
    /// </summary>
    [Test]
    public void IsValid_DomainLabelOf63Characters_ReturnsTrue()
    {
        // Arrange
        var email = "user@" + new string('a', 63) + ".com";

        // Act
        var isValid = EmailRules.IsValid(email);

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// A domain label one character over the 63-character maximum is rejected.
    /// </summary>
    [Test]
    public void IsValid_DomainLabelOf64Characters_ReturnsFalse()
    {
        // Arrange
        var email = "user@" + new string('a', 64) + ".com";

        // Act
        var isValid = EmailRules.IsValid(email);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// An address of exactly 254 characters, the maximum, is accepted.
    /// </summary>
    [Test]
    public void IsValid_EmailOf254Characters_ReturnsTrue()
    {
        // Arrange: pad the domain so the whole address is exactly 254 characters
        var email = "user@" + DomainOfLength(249);

        // Act
        var isValid = EmailRules.IsValid(email);

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// An address one character over the 254-character maximum is rejected, even though every part is valid.
    /// </summary>
    [Test]
    public void IsValid_EmailOver254Characters_ReturnsFalse()
    {
        // Arrange: pad the domain so the whole address is 255 characters
        var email = "user@" + DomainOfLength(250);

        // Act
        var isValid = EmailRules.IsValid(email);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// Builds a valid domain of exactly <paramref name="length"/> characters out of 63-character labels and a <c>com</c> TLD.
    /// </summary>
    private static string DomainOfLength(int length)
    {
        const string tld = ".com";
        var remaining = length - tld.Length;
        var labels = new List<string>();

        while (remaining > 0)
        {
            var labelLength = Math.Min(63, remaining);
            if (remaining - labelLength == 1)
                labelLength--;

            labels.Add(new string('a', labelLength));
            remaining -= labelLength + 1;
        }

        return string.Join('.', labels) + tld;
    }
}
