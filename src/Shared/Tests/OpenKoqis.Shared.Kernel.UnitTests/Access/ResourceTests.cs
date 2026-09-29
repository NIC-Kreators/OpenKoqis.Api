using AwesomeAssertions;
using OpenKoqis.Shared.Kernel.Access;

namespace OpenKoqis.Shared.Kernel.UnitTests.Access;

public class ResourceTests
{
    [Test]
    public void CommonPermissions_ContainsReadWriteEditDelete()
    {
        Resource.CommonPermissions.Should().BeEquivalentTo(["read", "write", "edit", "delete"]);
    }

    [Test]
    public void Constructor_NameOnly_UsesCommonPermissions()
    {
        var resource = new Resource("bins");

        resource.Name.Should().Be("bins");
        resource.AllowedPermissions.Should().BeEquivalentTo(Resource.CommonPermissions);
    }

    [Test]
    public void Constructor_MixedCaseAndWhitespace_NormalizesNameAndPermissions()
    {
        var resource = new Resource(" Bins ", [" Read", "WRITE "]);

        resource.Name.Should().Be("bins");
        resource.AllowedPermissions.Should().BeEquivalentTo(["read", "write"]);
    }

    [Test]
    public void Constructor_DuplicatePermissions_AreDeduplicated()
    {
        var resource = new Resource("bins", ["read", "READ", " read "]);

        resource.AllowedPermissions.Should().BeEquivalentTo(["read"]);
    }

    [Test]
    public void Constructor_NameOf32Characters_Succeeds()
    {
        var name = new string('a', 32);

        var resource = new Resource(name);

        resource.Name.Should().Be(name);
    }

    [Test]
    public void Constructor_NullName_Throws()
    {
        var act = () => new Resource(null!);

        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public void Constructor_EmptyOrWhiteSpaceName_Throws(string name)
    {
        var act = () => new Resource(name);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Test]
    [Arguments("bin_s")]
    [Arguments("1bins")]
    [Arguments("bins-")]
    [Arguments("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void Constructor_InvalidName_Throws(string name)
    {
        var act = () => new Resource(name);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Test]
    public void Constructor_NoPermissions_Throws()
    {
        var act = () => new Resource("bins", []);

        act.Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments("read_only")]
    [Arguments("")]
    [Arguments("aaaaaaaaaaaaaaaaa")]
    public void Constructor_InvalidPermission_Throws(string permission)
    {
        var act = () => new Resource("bins", ["read", permission]);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Test]
    public void Equals_SamePermissionsInDifferentOrder_ReturnsTrue()
    {
        var left = new Resource("bins", ["read", "write"]);
        var right = new Resource("BINS", ["write", "read"]);

        left.Should().Be(right);
        (left == right).Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Test]
    public void Equals_DifferentPermissions_ReturnsFalse()
    {
        var left = new Resource("bins", ["read", "write"]);
        var right = new Resource("bins", ["read"]);

        left.Should().NotBe(right);
        (left != right).Should().BeTrue();
    }

    [Test]
    public void Equals_DifferentName_ReturnsFalse()
    {
        var left = new Resource("bins");
        var right = new Resource("users");

        left.Should().NotBe(right);
    }
}
