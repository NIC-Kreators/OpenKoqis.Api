using AwesomeAssertions;

namespace OpenKoqis.Shared.Kernel.UnitTests;

public class EntityTests
{
    private sealed class Order(int id, string note) : Entity<int>(id)
    {
        public string Note { get; } = note;
    }

    private sealed class Invoice(int id) : Entity<int>(id);

    private sealed class Ticket(Guid id) : Entity<Guid>(id);

    private sealed class Tag(string id) : Entity<string>(id);

    /// <summary>
    /// A default <see langword="int"/> id is rejected with <see cref="ArgumentException"/> naming the <c>id</c> parameter.
    /// </summary>
    [Test]
    public void Constructor_DefaultIntId_Throws()
    {
        // Act
        var act = () => new Order(0, "note");

        // Assert
        act.Should().ThrowExactly<ArgumentException>().WithParameterName("id");
    }

    /// <summary>
    /// <see cref="Guid.Empty"/> as an id is rejected with <see cref="ArgumentException"/> naming the <c>id</c> parameter.
    /// </summary>
    [Test]
    public void Constructor_EmptyGuidId_Throws()
    {
        // Act
        var act = () => new Ticket(Guid.Empty);

        // Assert
        act.Should().ThrowExactly<ArgumentException>().WithParameterName("id");
    }

    /// <summary>
    /// A <see langword="null"/> reference-type id is rejected with an <see cref="ArgumentException"/>
    /// naming the <c>id</c> parameter, not a <see cref="NullReferenceException"/>.
    /// </summary>
    [Test]
    public void Constructor_NullReferenceId_Throws()
    {
        // Act
        var act = () => new Tag(null!);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    /// <summary>
    /// Non-default ids of value and reference types are accepted.
    /// </summary>
    [Test]
    public void Constructor_NonDefaultId_Succeeds()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var ticket = new Ticket(guid);
        var tag = new Tag("urgent");

        // Assert
        ticket.Id.Should().Be(guid);
        tag.Id.Should().Be("urgent");
    }

    /// <summary>
    /// Two entities of the same type with the same id are equal through every equality route,
    /// even when the rest of their state differs.
    /// </summary>
    [Test]
    public void Equals_SameTypeAndId_ReturnsTrueRegardlessOfOtherState()
    {
        // Arrange
        var left = new Order(1, "first");
        var right = new Order(1, "second");

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
    /// Two entities of the same type with different ids are not equal, even when the rest of their state matches.
    /// </summary>
    [Test]
    public void Equals_SameTypeDifferentId_ReturnsFalse()
    {
        // Arrange
        var left = new Order(1, "note");
        var right = new Order(2, "note");

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
    /// An entity is equal to itself.
    /// </summary>
    [Test]
    public void Equals_SameInstance_ReturnsTrue()
    {
        // Arrange
        var order = new Order(1, "note");

        // Act
        var typedEquals = order.Equals(order);
        var objectEquals = order.Equals((object)order);

        // Assert
        typedEquals.Should().BeTrue();
        objectEquals.Should().BeTrue();
    }

    /// <summary>
    /// An entity is never equal to <see langword="null"/>, whichever side of the comparison the null is on.
    /// </summary>
    [Test]
    public void Equals_Null_ReturnsFalse()
    {
        // Arrange
        var order = new Order(1, "note");
        Order? nothing = null;

        // Act
        var typedEquals = order.Equals(nothing);
        var objectEquals = order.Equals((object?)nothing);
        var equalityOperator = order == nothing;
        var reversedEqualityOperator = nothing == order;
        var inequalityOperator = order != nothing;

        // Assert
        typedEquals.Should().BeFalse();
        objectEquals.Should().BeFalse();
        equalityOperator.Should().BeFalse();
        reversedEqualityOperator.Should().BeFalse();
        inequalityOperator.Should().BeTrue();
    }

    /// <summary>
    /// The equality operators treat two <see langword="null"/> entities as equal.
    /// </summary>
    [Test]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        // Arrange
        Order? left = null;
        Order? right = null;

        // Act
        var equalityOperator = left == right;
        var inequalityOperator = left != right;

        // Assert
        equalityOperator.Should().BeTrue();
        inequalityOperator.Should().BeFalse();
    }

    /// <summary>
    /// Entities of different runtime types are not equal through <see cref="object.Equals(object)"/>
    /// or <c>==</c>, even when their ids match.
    /// </summary>
    [Test]
    public void Equals_DifferentRuntimeTypeWithSameId_ReturnsFalse()
    {
        // Arrange
        Entity<int> order = new Order(1, "note");
        Entity<int> invoice = new Invoice(1);

        // Act
        var objectEquals = order.Equals((object)invoice);
        var equalityOperator = order == invoice;

        // Assert
        objectEquals.Should().BeFalse();
        equalityOperator.Should().BeFalse();
    }

    /// <summary>
    /// The typed <see cref="IEquatable{T}.Equals(T)"/> gives the same answer as <see cref="object.Equals(object)"/>
    /// for entities of different runtime types with the same id.
    /// </summary>
    [Test]
    public void TypedEquals_DifferentRuntimeTypeWithSameId_AgreesWithObjectEquals()
    {
        // Arrange
        Entity<int> order = new Order(1, "note");
        Entity<int> invoice = new Invoice(1);

        // Act
        var typedEquals = order.Equals(invoice);
        var objectEquals = order.Equals((object)invoice);

        // Assert
        typedEquals.Should().Be(objectEquals);
    }

    /// <summary>
    /// Entities with the same id produce the same hash code, regardless of their other state.
    /// </summary>
    [Test]
    public void GetHashCode_SameId_ReturnsSameHash()
    {
        // Arrange
        var left = new Order(1, "first");
        var right = new Order(1, "second");

        // Act
        var leftHash = left.GetHashCode();
        var rightHash = right.GetHashCode();

        // Assert
        leftHash.Should().Be(rightHash);
    }

    /// <summary>
    /// A <see cref="HashSet{T}"/> keeps one entity per id.
    /// </summary>
    [Test]
    public void HashSet_EntitiesWithSameId_AreDeduplicated()
    {
        // Arrange
        Order[] orders = [new(1, "first"), new(1, "second"), new(2, "third")];

        // Act
        var set = new HashSet<Order>(orders);

        // Assert
        set.Should().HaveCount(2);
    }
}
