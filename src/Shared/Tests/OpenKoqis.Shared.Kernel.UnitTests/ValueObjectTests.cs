using AwesomeAssertions;

namespace OpenKoqis.Shared.Kernel.UnitTests;

public class ValueObjectTests
{
    private sealed class Money(decimal amount, string? currency) : ValueObject
    {
        public decimal Amount { get; } = amount;
        public string? Currency { get; } = currency;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency!;
        }
    }

    private sealed class Price(decimal amount, string? currency) : ValueObject
    {
        public decimal Amount { get; } = amount;
        public string? Currency { get; } = currency;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency!;
        }
    }

    /// <summary>
    /// Two value objects of the same type with equal components are equal through every equality route.
    /// </summary>
    [Test]
    public void Equals_SameComponents_ReturnsTrue()
    {
        // Arrange
        var left = new Money(10m, "USD");
        var right = new Money(10m, "USD");

        // Act
        var typedEquals = left.Equals(right);
        var objectEquals = left.Equals((object)right);
        var equalityOperator = left == right;
        var inequalityOperator = left != right;

        // Assert
        typedEquals.Should().BeTrue();
        objectEquals.Should().BeTrue();
        equalityOperator.Should().BeTrue();
        inequalityOperator.Should().BeFalse();
    }

    /// <summary>
    /// Value objects that differ in any single component are not equal.
    /// </summary>
    [Test]
    [Arguments(10, "EUR")]
    [Arguments(11, "USD")]
    public void Equals_DifferentComponents_ReturnsFalse(int amount, string currency)
    {
        // Arrange
        var left = new Money(10m, "USD");
        var right = new Money(amount, currency);

        // Act
        var typedEquals = left.Equals(right);
        var objectEquals = left.Equals((object)right);
        var equalityOperator = left == right;
        var inequalityOperator = left != right;

        // Assert
        typedEquals.Should().BeFalse();
        objectEquals.Should().BeFalse();
        equalityOperator.Should().BeFalse();
        inequalityOperator.Should().BeTrue();
    }

    /// <summary>
    /// <see langword="null"/> components compare equal to each other and hash consistently.
    /// </summary>
    [Test]
    public void Equals_NullComponentsOnBothSides_ReturnsTrue()
    {
        // Arrange
        var left = new Money(10m, null);
        var right = new Money(10m, null);

        // Act
        var equals = left.Equals(right);
        var leftHash = left.GetHashCode();
        var rightHash = right.GetHashCode();

        // Assert
        equals.Should().BeTrue();
        leftHash.Should().Be(rightHash);
    }

    /// <summary>
    /// A <see langword="null"/> component is not equal to a non-null one, in either direction.
    /// </summary>
    [Test]
    public void Equals_NullComponentOnOneSide_ReturnsFalse()
    {
        // Arrange
        var left = new Money(10m, null);
        var right = new Money(10m, "USD");

        // Act
        var leftEqualsRight = left.Equals(right);
        var rightEqualsLeft = right.Equals(left);

        // Assert
        leftEqualsRight.Should().BeFalse();
        rightEqualsLeft.Should().BeFalse();
    }

    /// <summary>
    /// A value object is never equal to <see langword="null"/>, whichever side of the comparison the null is on.
    /// </summary>
    [Test]
    public void Equals_Null_ReturnsFalse()
    {
        // Arrange
        var money = new Money(10m, "USD");
        Money nothing = null!;

        // Act
        var typedEquals = money.Equals(nothing);
        var objectEquals = money.Equals((object?)nothing);
        var equalityOperator = money == nothing;
        var reversedEqualityOperator = nothing == money;
        var inequalityOperator = money != nothing;

        // Assert
        typedEquals.Should().BeFalse();
        objectEquals.Should().BeFalse();
        equalityOperator.Should().BeFalse();
        reversedEqualityOperator.Should().BeFalse();
        inequalityOperator.Should().BeTrue();
    }

    /// <summary>
    /// The equality operators treat two <see langword="null"/> value objects as equal.
    /// </summary>
    [Test]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        // Arrange
        Money left = null!;
        Money right = null!;

        // Act
        var equalityOperator = left == right;
        var inequalityOperator = left != right;

        // Assert
        equalityOperator.Should().BeTrue();
        inequalityOperator.Should().BeFalse();
    }

    /// <summary>
    /// Value objects of different runtime types are not equal through <see cref="object.Equals(object)"/>
    /// or <c>==</c>, even when their components match.
    /// </summary>
    [Test]
    public void Equals_DifferentRuntimeTypeWithSameComponents_ReturnsFalse()
    {
        // Arrange
        ValueObject money = new Money(10m, "USD");
        ValueObject price = new Price(10m, "USD");

        // Act
        var objectEquals = money.Equals((object)price);
        var equalityOperator = money == price;

        // Assert
        objectEquals.Should().BeFalse();
        equalityOperator.Should().BeFalse();
    }

    /// <summary>
    /// The typed <see cref="IEquatable{T}.Equals(T)"/> gives the same answer as <see cref="object.Equals(object)"/>
    /// for value objects of different runtime types with the same components.
    /// </summary>
    [Test]
    public void TypedEquals_DifferentRuntimeTypeWithSameComponents_AgreesWithObjectEquals()
    {
        // Arrange
        ValueObject money = new Money(10m, "USD");
        ValueObject price = new Price(10m, "USD");

        // Act
        var typedEquals = money.Equals(price);
        var objectEquals = money.Equals((object)price);

        // Assert
        typedEquals.Should().Be(objectEquals);
    }

    /// <summary>
    /// Value objects with equal components produce the same hash code.
    /// </summary>
    [Test]
    public void GetHashCode_SameComponents_ReturnsSameHash()
    {
        // Arrange
        var left = new Money(10m, "USD");
        var right = new Money(10m, "USD");

        // Act
        var leftHash = left.GetHashCode();
        var rightHash = right.GetHashCode();

        // Assert
        leftHash.Should().Be(rightHash);
    }

    /// <summary>
    /// A <see cref="HashSet{T}"/> keeps one value object per set of equal components.
    /// </summary>
    [Test]
    public void HashSet_EqualValueObjects_AreDeduplicated()
    {
        // Arrange
        Money[] values = [new(10m, "USD"), new(10m, "USD"), new(10m, "EUR")];

        // Act
        var set = new HashSet<Money>(values);

        // Assert
        set.Should().HaveCount(2);
    }
}
