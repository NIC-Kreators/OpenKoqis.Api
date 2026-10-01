using AwesomeAssertions;
using OpenKoqis.Shared.Kernel.Access;

namespace OpenKoqis.Shared.Kernel.UnitTests.Access;

public class ResourceTests
{
    /// <summary>
    /// The name-only constructor allows the <see cref="Resource.CommonPermissions"/>.
    /// </summary>
    [Test]
    public void Constructor_NameOnly_UsesCommonPermissions()
    {
        // Act
        var resource = new Resource("bins");

        // Assert
        resource.Name.Should().Be("bins");
        resource.AllowedPermissions.Should().BeEquivalentTo(Resource.CommonPermissions);
    }

    /// <summary>
    /// The name and the permissions are stored lowercased and trimmed.
    /// </summary>
    [Test]
    public void Constructor_MixedCaseAndWhitespace_NormalizesNameAndPermissions()
    {
        // Act
        var resource = new Resource(" Bins ", [" Read", "WRITE "]);

        // Assert
        resource.Name.Should().Be("bins");
        resource.AllowedPermissions.Should().BeEquivalentTo(["read", "write"]);
    }

    /// <summary>
    /// Permissions that are equal after normalization are stored once.
    /// </summary>
    [Test]
    public void Constructor_DuplicatePermissions_AreDeduplicated()
    {
        // Act
        var resource = new Resource("bins", ["read", "READ", " read "]);

        // Assert
        resource.AllowedPermissions.Should().BeEquivalentTo(["read"]);
    }

    /// <summary>
    /// A name of exactly 32 characters, the maximum, is accepted.
    /// </summary>
    [Test]
    public void Constructor_NameOf32Characters_Succeeds()
    {
        // Arrange
        var name = new string('a', 32);

        // Act
        var resource = new Resource(name);

        // Assert
        resource.Name.Should().Be(name);
    }

    /// <summary>
    /// A permission of exactly 16 characters, the maximum, is accepted.
    /// </summary>
    [Test]
    public void Constructor_PermissionOf16Characters_Succeeds()
    {
        // Arrange
        var permission = new string('a', 16);

        // Act
        var resource = new Resource("bins", [permission]);

        // Assert
        resource.AllowedPermissions.Should().BeEquivalentTo([permission]);
    }

    /// <summary>
    /// A <see langword="null"/> name throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [Test]
    public void Constructor_NullName_Throws()
    {
        // Act
        var act = () => new Resource(null!);

        // Assert
        act.Should().ThrowExactly<ArgumentNullException>();
    }

    /// <summary>
    /// An empty or whitespace-only name throws <see cref="ArgumentException"/>.
    /// </summary>
    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public void Constructor_EmptyOrWhiteSpaceName_Throws(string name)
    {
        // Act
        var act = () => new Resource(name);

        // Assert
        act.Should().ThrowExactly<ArgumentException>();
    }

    /// <summary>
    /// A name that breaks the system-name rules, or is longer than 32 characters, throws <see cref="ArgumentException"/>.
    /// </summary>
    [Test]
    [Arguments("bin_s")]
    [Arguments("1bins")]
    [Arguments("bins-")]
    [Arguments("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void Constructor_InvalidName_Throws(string name)
    {
        // Act
        var act = () => new Resource(name);

        // Assert
        act.Should().ThrowExactly<ArgumentException>();
    }

    /// <summary>
    /// An empty permission list throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Test]
    public void Constructor_NoPermissions_Throws()
    {
        // Act
        var act = () => new Resource("bins", []);

        // Assert
        act.Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// A permission that breaks the system-name rules, is blank or is longer than 16 characters
    /// throws <see cref="ArgumentException"/>, even when the other permissions are valid.
    /// </summary>
    [Test]
    [Arguments("read_only")]
    [Arguments("")]
    [Arguments("aaaaaaaaaaaaaaaaa")]
    public void Constructor_InvalidPermission_Throws(string permission)
    {
        // Act
        var act = () => new Resource("bins", ["read", permission]);

        // Assert
        act.Should().ThrowExactly<ArgumentException>();
    }

    /// <summary>
    /// Resources are equal, and hash the same, when their normalized name and permission sets match,
    /// whatever order the permissions were given in.
    /// </summary>
    [Test]
    public void Equals_SamePermissionsInDifferentOrder_ReturnsTrue()
    {
        // Arrange
        var left = new Resource("bins", ["read", "write"]);
        var right = new Resource("BINS", ["write", "read"]);

        // Act
        var equalityOperator = left == right;
        var leftHash = left.GetHashCode();
        var rightHash = right.GetHashCode();

        // Assert
        left.Should().Be(right);
        equalityOperator.Should().BeTrue();
        leftHash.Should().Be(rightHash);
    }

    /// <summary>
    /// Resources with the same name but different allowed permissions are not equal.
    /// </summary>
    [Test]
    public void Equals_DifferentPermissions_ReturnsFalse()
    {
        // Arrange
        var left = new Resource("bins", ["read", "write"]);
        var right = new Resource("bins", ["read"]);

        // Act
        var inequalityOperator = left != right;

        // Assert
        left.Should().NotBe(right);
        inequalityOperator.Should().BeTrue();
    }

    /// <summary>
    /// Resources with the same allowed permissions but different names are not equal.
    /// </summary>
    [Test]
    public void Equals_DifferentName_ReturnsFalse()
    {
        // Arrange
        var left = new Resource("bins");
        var right = new Resource("users");

        // Act
        var equals = left.Equals(right);

        // Assert
        equals.Should().BeFalse();
    }
}
