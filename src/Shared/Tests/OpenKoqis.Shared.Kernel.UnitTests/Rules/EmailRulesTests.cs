using AwesomeAssertions;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Shared.Kernel.UnitTests.Rules;

public class EmailRulesTests
{
    [Test]
    [Arguments("a@b.co")]
    [Arguments("john.doe@example.com")]
    [Arguments("user+tag@sub.example.org")]
    [Arguments("first_last@ex-ample.com")]
    [Arguments("o'neil@example.io")]
    [Arguments("x!#$%&'*+/=?^_`{|}~-y@example.com")]
    [Arguments("user@123.example.com")]
    public void IsValid_ValidEmail_ReturnsTrue(string email)
        => EmailRules.IsValid(email).Should().BeTrue();

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
        => EmailRules.IsValid(email).Should().BeFalse();

    [Test]
    public void IsValid_LocalPartOf64Characters_ReturnsTrue()
    {
        var email = new string('a', 64) + "@example.com";

        EmailRules.IsValid(email).Should().BeTrue();
    }

    [Test]
    public void IsValid_LocalPartOf65Characters_ReturnsFalse()
    {
        var email = new string('a', 65) + "@example.com";

        EmailRules.IsValid(email).Should().BeFalse();
    }

    [Test]
    public void IsValid_DomainLabelOf63Characters_ReturnsTrue()
    {
        var email = "user@" + new string('a', 63) + ".com";

        EmailRules.IsValid(email).Should().BeTrue();
    }

    [Test]
    public void IsValid_DomainLabelOf64Characters_ReturnsFalse()
    {
        var email = "user@" + new string('a', 64) + ".com";

        EmailRules.IsValid(email).Should().BeFalse();
    }

    [Test]
    public void IsValid_EmailOf254Characters_ReturnsTrue()
    {
        var email = "user@" + DomainOfLength(249);

        email.Should().HaveLength(254);
        EmailRules.IsValid(email).Should().BeTrue();
    }

    [Test]
    public void IsValid_EmailOver254Characters_ReturnsFalse()
    {
        var email = "user@" + DomainOfLength(250);

        email.Should().HaveLength(255);
        EmailRules.IsValid(email).Should().BeFalse();
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
