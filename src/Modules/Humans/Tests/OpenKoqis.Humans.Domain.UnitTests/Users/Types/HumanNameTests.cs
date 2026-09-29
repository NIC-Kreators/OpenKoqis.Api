using AwesomeAssertions;
using OpenKoqis.Humans.Domain.Users.Types;

namespace OpenKoqis.Humans.Domain.UnitTests.Users.Types;

public class HumanNameTests
{
    [Test]
    public void Parse_OneWord_SetsFirstNameOnly()
    {
        var name = HumanName.Parse("Ivan").Value;

        name.FirstName.Should().Be("Ivan");
        name.LastName.Should().BeNull();
        name.MiddleName.Should().BeNull();
        name.FullName.Should().Be("Ivan");
    }

    [Test]
    public void Parse_TwoWords_SetsFirstAndLastName()
    {
        var name = HumanName.Parse("Ivan Ivanov").Value;

        name.FirstName.Should().Be("Ivan");
        name.LastName.Should().Be("Ivanov");
        name.MiddleName.Should().BeNull();
        name.FullName.Should().Be("Ivan Ivanov");
    }

    [Test]
    public void Parse_ThreeWords_SetsFirstLastAndMiddleName()
    {
        var name = HumanName.Parse("Иван Иванов Иванович").Value;

        name.FirstName.Should().Be("Иван");
        name.LastName.Should().Be("Иванов");
        name.MiddleName.Should().Be("Иванович");
        name.FullName.Should().Be("Иван Иванов Иванович");
    }

    [Test]
    public void Parse_ExtraSpaces_AreCollapsed()
        => HumanName.Parse("  Ivan    Ivanov  ").Value.FullName.Should().Be("Ivan Ivanov");

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public void Parse_EmptyOrWhiteSpace_ReturnsWrongAmountOfWords(string raw)
    {
        var result = HumanName.Parse(raw);

        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("HumanName.WrongAmountOfWords");
        result.FirstError.Description.Should().EndWith("Found: 0");
    }

    [Test]
    public void Parse_FourWords_ReturnsWrongAmountOfWords()
    {
        var result = HumanName.Parse("Ivan Ivanov Ivanovich Jr");

        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("HumanName.WrongAmountOfWords");
        result.FirstError.Description.Should().EndWith("Found: 4");
    }

    [Test]
    [Arguments("Anna-Maria")]
    [Arguments("O'Brien")]
    [Arguments("Ivan2")]
    [Arguments("Ivan Ivanov.")]
    [Arguments("Abcdefghijklmnopq")]
    public void Parse_WordWithNonLettersOrTooLong_ReturnsAllSymbolsShouldBeALetter(string raw)
        => HumanName.Parse(raw).Errors.Should().ContainSingle()
            .Which.Code.Should().Be("HumanName.AllSymbolsShouldBeALetter");

    [Test]
    public void Parse_TooManyInvalidWords_ReturnsBothErrors()
        => HumanName.Parse("A1 B2 C3 D4").Errors.Select(e => e.Code).Should().BeEquivalentTo(
            "HumanName.WrongAmountOfWords",
            "HumanName.AllSymbolsShouldBeALetter");

    [Test]
    public void Equals_SameWordsWithDifferentSpacing_ReturnsTrue()
    {
        var left = HumanName.Parse("Ivan Ivanov").Value;
        var right = HumanName.Parse("  Ivan   Ivanov ").Value;

        left.Should().Be(right);
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Test]
    public void Equals_SameWordsInDifferentOrder_ReturnsFalse()
        => HumanName.Parse("Ivan Ivanov").Value.Should().NotBe(HumanName.Parse("Ivanov Ivan").Value);
}
