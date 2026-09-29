using AwesomeAssertions;
using OpenKoqis.Shared.Kernel.Rules;

namespace OpenKoqis.Shared.Kernel.UnitTests.Rules;

public class NameRulesTests
{
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
        => NameRules.IsValid(name).Should().BeTrue();

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
        => NameRules.IsValid(name).Should().BeFalse();

    [Test]
    public void IsValid_NameAtCustomMaxLength_ReturnsTrue()
    {
        var name = new string('a', 32);

        NameRules.IsValid(name, maxLength: 32).Should().BeTrue();
    }

    [Test]
    public void IsValid_NameOverCustomMaxLength_ReturnsFalse()
        => NameRules.IsValid("Anna", maxLength: 3).Should().BeFalse();
}
