using AwesomeAssertions;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Shared.Kernel.UnitTests.Rules;

public class SystemNameRulesTests
{
    [Test]
    [Arguments("a")]
    [Arguments("bins")]
    [Arguments("bin2")]
    [Arguments("cleaning-log")]
    [Arguments("a-b-c")]
    [Arguments("bin*")]
    [Arguments("abcdefghijklmnop")]
    public void IsValid_KebabCaseName_ReturnsTrue(string name)
        => SystemNameRules.IsValid(name).Should().BeTrue();

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
        => SystemNameRules.IsValid(name).Should().BeFalse();

    [Test]
    public void IsValid_NameAtCustomMaxLength_ReturnsTrue()
    {
        var name = new string('a', 32);

        SystemNameRules.IsValid(name, maxLength: 32).Should().BeTrue();
    }

    [Test]
    public void IsValid_NameOverCustomMaxLength_ReturnsFalse()
    {
        var name = new string('a', 33);

        SystemNameRules.IsValid(name, maxLength: 32).Should().BeFalse();
    }

    [Test]
    public void IsValid_NameOverDefaultMaxLength_ReturnsFalse()
    {
        var name = new string('a', 17);

        SystemNameRules.IsValid(name).Should().BeFalse();
    }
}
