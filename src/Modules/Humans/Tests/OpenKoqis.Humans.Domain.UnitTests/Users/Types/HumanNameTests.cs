using AwesomeAssertions;
using OpenKoqis.Humans.Domain.Users.Types;

namespace OpenKoqis.Humans.Domain.UnitTests.Users.Types;

public class HumanNameTests
{
    /// <summary>
    /// A single word becomes the first name; the last and middle names stay <see langword="null"/>.
    /// </summary>
    [Test]
    public void Parse_OneWord_SetsFirstNameOnly()
    {
        // Act
        var name = HumanName.Parse("Ivan").Value;

        // Assert
        name.FirstName.Should().Be("Ivan");
        name.LastName.Should().BeNull();
        name.MiddleName.Should().BeNull();
        name.FullName.Should().Be("Ivan");
    }

    /// <summary>
    /// Two words become the first and last names.
    /// </summary>
    [Test]
    public void Parse_TwoWords_SetsFirstAndLastName()
    {
        // Act
        var name = HumanName.Parse("Ivan Ivanov").Value;

        // Assert
        name.FirstName.Should().Be("Ivan");
        name.LastName.Should().Be("Ivanov");
        name.MiddleName.Should().BeNull();
        name.FullName.Should().Be("Ivan Ivanov");
    }

    /// <summary>
    /// Three words become the first, last and middle names, in that order; non-Latin letters are accepted.
    /// </summary>
    [Test]
    public void Parse_ThreeWords_SetsFirstLastAndMiddleName()
    {
        // Act
        var name = HumanName.Parse("Иван Иванов Иванович").Value;

        // Assert
        name.FirstName.Should().Be("Иван");
        name.LastName.Should().Be("Иванов");
        name.MiddleName.Should().Be("Иванович");
        name.FullName.Should().Be("Иван Иванов Иванович");
    }

    /// <summary>
    /// Leading, trailing and repeated spaces are dropped, so <see cref="HumanName.FullName"/> has single spaces.
    /// </summary>
    [Test]
    public void Parse_ExtraSpaces_AreCollapsed()
    {
        // Act
        var name = HumanName.Parse("  Ivan    Ivanov  ").Value;

        // Assert
        name.FullName.Should().Be("Ivan Ivanov");
    }

    /// <summary>
    /// A word of exactly 16 letters, the maximum, is accepted.
    /// </summary>
    [Test]
    public void Parse_WordOf16Letters_Succeeds()
    {
        // Act
        var name = HumanName.Parse("Ivan Abcdefghijklmnop").Value;

        // Assert
        name.LastName.Should().Be("Abcdefghijklmnop");
    }

    /// <summary>
    /// Empty or whitespace-only input returns a single <c>HumanName.WrongAmountOfWords</c> error reporting zero words.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public void Parse_EmptyOrWhiteSpace_ReturnsWrongAmountOfWords(string raw)
    {
        // Act
        var result = HumanName.Parse(raw);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("HumanName.WrongAmountOfWords");
        result.FirstError.Description.Should().EndWith("Found: 0");
    }

    /// <summary>
    /// More than three words returns a single <c>HumanName.WrongAmountOfWords</c> error reporting the word count.
    /// </summary>
    [Test]
    public void Parse_FourWords_ReturnsWrongAmountOfWords()
    {
        // Act
        var result = HumanName.Parse("Ivan Ivanov Ivanovich Jr");

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("HumanName.WrongAmountOfWords");
        result.FirstError.Description.Should().EndWith("Found: 4");
    }

    /// <summary>
    /// A word containing a hyphen, apostrophe, digit or punctuation, or longer than 16 letters,
    /// returns a single <c>HumanName.AllSymbolsShouldBeALetter</c> error.
    /// </summary>
    [Test]
    [Arguments("Anna-Maria")]
    [Arguments("O'Brien")]
    [Arguments("Ivan2")]
    [Arguments("Ivan Ivanov.")]
    [Arguments("Abcdefghijklmnopq")]
    public void Parse_WordWithNonLettersOrTooLong_ReturnsAllSymbolsShouldBeALetter(string raw)
    {
        // Act
        var result = HumanName.Parse(raw);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("HumanName.AllSymbolsShouldBeALetter");
    }

    /// <summary>
    /// Too many words that are also invalid report both the word-count and the letters-only errors.
    /// </summary>
    [Test]
    public void Parse_TooManyInvalidWords_ReturnsBothErrors()
    {
        // Act
        var result = HumanName.Parse("A1 B2 C3 D4");

        // Assert
        result.Errors.Select(e => e.Code).Should().BeEquivalentTo(
            "HumanName.WrongAmountOfWords",
            "HumanName.AllSymbolsShouldBeALetter");
    }

    /// <summary>
    /// Names made of the same words are equal, and hash the same, regardless of the spacing they were parsed from.
    /// </summary>
    [Test]
    public void Equals_SameWordsWithDifferentSpacing_ReturnsTrue()
    {
        // Arrange
        var left = HumanName.Parse("Ivan Ivanov").Value;
        var right = HumanName.Parse("  Ivan   Ivanov ").Value;

        // Act
        var leftHash = left.GetHashCode();
        var rightHash = right.GetHashCode();

        // Assert
        left.Should().Be(right);
        leftHash.Should().Be(rightHash);
    }

    /// <summary>
    /// Word order matters: swapping the first and last names gives a different name.
    /// </summary>
    [Test]
    public void Equals_SameWordsInDifferentOrder_ReturnsFalse()
    {
        // Arrange
        var left = HumanName.Parse("Ivan Ivanov").Value;
        var right = HumanName.Parse("Ivanov Ivan").Value;

        // Act
        var equals = left.Equals(right);

        // Assert
        equals.Should().BeFalse();
    }
}
