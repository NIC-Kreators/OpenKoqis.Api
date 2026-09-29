using AwesomeAssertions;

namespace OpenKoqis.Shared.Kernel.UnitTests;

public class EntityTests
{
    private sealed class Order(int id, string note) : Entity<int>(id)
    {
        public string Note { get; } = note;
    }

    private sealed class Invoice(int id) : Entity<int>(id);

    [Test]
    public void Equals_SameTypeAndId_ReturnsTrueRegardlessOfOtherState()
    {
        var left = new Order(1, "first");
        var right = new Order(1, "second");

        left.Equals(right).Should().BeTrue();
        left.Equals((object)right).Should().BeTrue();
        (left == right).Should().BeTrue();
        (left != right).Should().BeFalse();
    }

    [Test]
    public void Equals_SameTypeDifferentId_ReturnsFalse()
    {
        var left = new Order(1, "note");
        var right = new Order(2, "note");

        left.Equals(right).Should().BeFalse();
        left.Equals((object)right).Should().BeFalse();
        (left == right).Should().BeFalse();
        (left != right).Should().BeTrue();
    }

    [Test]
    public void Equals_SameInstance_ReturnsTrue()
    {
        var order = new Order(1, "note");

        order.Equals(order).Should().BeTrue();
        order.Equals((object)order).Should().BeTrue();
    }

    [Test]
    public void Equals_Null_ReturnsFalse()
    {
        var order = new Order(1, "note");
        Order? nothing = null;

        order.Equals(nothing).Should().BeFalse();
        order.Equals((object?)nothing).Should().BeFalse();
        (order == nothing).Should().BeFalse();
        (nothing == order).Should().BeFalse();
        (order != nothing).Should().BeTrue();
    }

    [Test]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        Order? left = null;
        Order? right = null;

        (left == right).Should().BeTrue();
        (left != right).Should().BeFalse();
    }

    [Test]
    public void Equals_DifferentRuntimeTypeWithSameId_ReturnsFalse()
    {
        Entity<int> order = new Order(1, "note");
        Entity<int> invoice = new Invoice(1);

        order.Equals((object)invoice).Should().BeFalse();
        (order == invoice).Should().BeFalse();
    }

    [Test]
    public void TypedEquals_DifferentRuntimeTypeWithSameId_AgreesWithObjectEquals()
    {
        Entity<int> order = new Order(1, "note");
        Entity<int> invoice = new Invoice(1);

        order.Equals(invoice).Should().Be(order.Equals((object)invoice));
    }

    [Test]
    public void GetHashCode_SameId_ReturnsSameHash()
    {
        var left = new Order(1, "first");
        var right = new Order(1, "second");

        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Test]
    public void HashSet_EntitiesWithSameId_AreDeduplicated()
    {
        var set = new HashSet<Order> { new(1, "first"), new(1, "second"), new(2, "third") };

        set.Should().HaveCount(2);
    }

    [Test]
    public void Id_ReturnsValuePassedToConstructor()
    {
        var order = new Order(42, "note");

        order.Id.Should().Be(42);
    }
}
