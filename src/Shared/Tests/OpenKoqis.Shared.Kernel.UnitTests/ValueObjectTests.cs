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

    [Test]
    public void Equals_SameComponents_ReturnsTrue()
    {
        var left = new Money(10m, "USD");
        var right = new Money(10m, "USD");

        left.Equals(right).Should().BeTrue();
        left.Equals((object)right).Should().BeTrue();
        (left == right).Should().BeTrue();
        (left != right).Should().BeFalse();
    }

    [Test]
    [Arguments(10, "EUR")]
    [Arguments(11, "USD")]
    public void Equals_DifferentComponents_ReturnsFalse(int amount, string currency)
    {
        var left = new Money(10m, "USD");
        var right = new Money(amount, currency);

        left.Equals(right).Should().BeFalse();
        left.Equals((object)right).Should().BeFalse();
        (left == right).Should().BeFalse();
        (left != right).Should().BeTrue();
    }

    [Test]
    public void Equals_NullComponentsOnBothSides_ReturnsTrue()
    {
        var left = new Money(10m, null);
        var right = new Money(10m, null);

        left.Equals(right).Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Test]
    public void Equals_NullComponentOnOneSide_ReturnsFalse()
    {
        var left = new Money(10m, null);
        var right = new Money(10m, "USD");

        left.Equals(right).Should().BeFalse();
        right.Equals(left).Should().BeFalse();
    }

    [Test]
    public void Equals_Null_ReturnsFalse()
    {
        var money = new Money(10m, "USD");
        Money nothing = null!;

        money.Equals(nothing).Should().BeFalse();
        money.Equals((object?)nothing).Should().BeFalse();
        (money == nothing).Should().BeFalse();
        (nothing == money).Should().BeFalse();
        (money != nothing).Should().BeTrue();
    }

    [Test]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        Money left = null!;
        Money right = null!;

        (left == right).Should().BeTrue();
        (left != right).Should().BeFalse();
    }

    [Test]
    public void Equals_DifferentRuntimeTypeWithSameComponents_ReturnsFalse()
    {
        ValueObject money = new Money(10m, "USD");
        ValueObject price = new Price(10m, "USD");

        money.Equals((object)price).Should().BeFalse();
        (money == price).Should().BeFalse();
    }

    [Test]
    public void TypedEquals_DifferentRuntimeTypeWithSameComponents_AgreesWithObjectEquals()
    {
        ValueObject money = new Money(10m, "USD");
        ValueObject price = new Price(10m, "USD");

        money.Equals(price).Should().Be(money.Equals((object)price));
    }

    [Test]
    public void GetHashCode_SameComponents_ReturnsSameHash()
    {
        var left = new Money(10m, "USD");
        var right = new Money(10m, "USD");

        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Test]
    public void HashSet_EqualValueObjects_AreDeduplicated()
    {
        var set = new HashSet<Money> { new(10m, "USD"), new(10m, "USD"), new(10m, "EUR") };

        set.Should().HaveCount(2);
    }
}
