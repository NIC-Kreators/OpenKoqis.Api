using AwesomeAssertions;
using ErrorOr;
using KernelAccess = OpenKoqis.Shared.Kernel.Access.Access;

namespace OpenKoqis.Shared.Kernel.UnitTests.Access;

public class AccessTests
{
    private static KernelAccess Create(string raw) => KernelAccess.Parse(raw).Value;

    [Test]
    public void Parse_SinglePermission_ReturnsAccess()
    {
        var result = KernelAccess.Parse("bins:read");

        result.IsError.Should().BeFalse();
        result.Value.Resource.Should().Be("bins");
        result.Value.Permissions.Should().BeEquivalentTo("read");
    }

    [Test]
    public void Parse_SeveralPermissions_ReturnsAllOfThem()
    {
        var result = KernelAccess.Parse("bins:read,write,delete");

        result.IsError.Should().BeFalse();
        result.Value.Permissions.Should().BeEquivalentTo("read", "write", "delete");
    }

    [Test]
    public void Parse_MixedCaseAndWhitespace_NormalizesResourceAndPermissions()
    {
        var result = KernelAccess.Parse(" Bins : Read , WRITE ");

        result.IsError.Should().BeFalse();
        result.Value.Resource.Should().Be("bins");
        result.Value.Permissions.Should().BeEquivalentTo("read", "write");
    }

    [Test]
    public void Parse_EmptyPermissionEntries_AreIgnored()
    {
        var result = KernelAccess.Parse("bins:read,, ,write");

        result.IsError.Should().BeFalse();
        result.Value.Permissions.Should().BeEquivalentTo("read", "write");
    }

    [Test]
    public void Parse_DuplicatePermissions_AreDeduplicated()
    {
        var result = KernelAccess.Parse("bins:read,READ, read");

        result.IsError.Should().BeFalse();
        result.Value.Permissions.Should().BeEquivalentTo("read");
    }

    [Test]
    [Arguments("")]
    [Arguments("bins")]
    [Arguments("bins:read:write")]
    [Arguments(":read")]
    [Arguments("   :read")]
    [Arguments("bins:")]
    [Arguments("bins: , ,")]
    public void Parse_MalformedAccessString_ReturnsInvalidAccessString(string raw)
    {
        var result = KernelAccess.Parse(raw);

        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.FirstError.Code.Should().Be("Access.InvalidAccessString");
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().EndWith($"Received: {raw}");
    }

    [Test]
    public void Parse_InvalidResource_ReturnsInvalidResource()
    {
        var result = KernelAccess.Parse("bin_s:read");

        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.FirstError.Code.Should().Be("Access.InvalidResource");
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().EndWith("Received: bin_s");
    }

    [Test]
    public void Parse_InvalidPermission_ReturnsInvalidPermission()
    {
        var result = KernelAccess.Parse("bins:read,Read_Only");

        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.FirstError.Code.Should().Be("Access.InvalidPermission");
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().EndWith("Received: read_only");
    }

    [Test]
    public void Parse_SeveralInvalidNames_ReturnsErrorForEach()
    {
        var result = KernelAccess.Parse("bin_s:re_ad,wr_ite");

        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().BeEquivalentTo(
            "Access.InvalidPermission", "Access.InvalidPermission", "Access.InvalidResource");
    }

    [Test]
    public void Parse_ResourceOf32Characters_ReturnsAccess()
    {
        var resource = new string('a', 32);

        var result = KernelAccess.Parse($"{resource}:read");

        result.IsError.Should().BeFalse();
        result.Value.Resource.Should().Be(resource);
    }

    [Test]
    public void Parse_ResourceOf33Characters_ReturnsInvalidResource()
    {
        var result = KernelAccess.Parse($"{new string('a', 33)}:read");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Access.InvalidResource");
    }

    [Test]
    public void Parse_PermissionOver16Characters_ReturnsInvalidPermission()
    {
        var result = KernelAccess.Parse($"bins:{new string('a', 17)}");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Access.InvalidPermission");
    }

    [Test]
    public void ParseSeparated_ValidInput_ReturnsNormalizedAccess()
    {
        var result = KernelAccess.Parse(" Bins ", [" Read", "WRITE "]);

        result.IsError.Should().BeFalse();
        result.Value.Resource.Should().Be("bins");
        result.Value.Permissions.Should().BeEquivalentTo(["read", "write"]);
    }

    [Test]
    public void ParseSeparated_NoPermissions_ReturnsNoPermissions()
    {
        var result = KernelAccess.Parse("bins", []);

        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.FirstError.Code.Should().Be("Access.NoPermissions");
        result.FirstError.Type.Should().Be(ErrorType.Validation);
    }

    [Test]
    public void ParseSeparated_EmptyPermission_ReturnsInvalidPermission()
    {
        var result = KernelAccess.Parse("bins", ["read", " "]);

        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.FirstError.Code.Should().Be("Access.InvalidPermission");
    }

    [Test]
    public void ParseSeparated_EmptyResourceAndNoPermissions_ReturnsBothErrors()
    {
        var result = KernelAccess.Parse("", []);

        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().BeEquivalentTo("Access.NoPermissions", "Access.InvalidResource");
    }

    [Test]
    public void Equals_SamePermissionsInDifferentOrder_ReturnsTrue()
    {
        var left = Create("bins:read,write");
        var right = Create("bins:write,read");

        left.Should().Be(right);
        (left == right).Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Test]
    [Arguments("bins:read")]
    [Arguments("bins:read,write,delete")]
    [Arguments("users:read,write")]
    public void Equals_DifferentResourceOrPermissions_ReturnsFalse(string other)
    {
        var access = Create("bins:read,write");

        access.Should().NotBe(Create(other));
        (access == Create(other)).Should().BeFalse();
    }

    [Test]
    public void Merge_AccessesOnSameResource_UnitesTheirPermissions()
    {
        var merged = KernelAccess.Merge(
        [
            Create("bins:read"),
            Create("bins:write"),
            Create("users:write"),
        ]);

        merged.Should().BeEquivalentTo([
            Create("bins:read,write"),
            Create("users:write")
        ]);
    }

    [Test]
    public void Merge_OverlappingPermissions_AreDeduplicated()
    {
        var merged = KernelAccess.Merge([Create("bins:read,write"), Create("bins:write,delete")]);

        merged.Should().ContainSingle()
            .Which.Permissions.Should().BeEquivalentTo("read", "write", "delete");
    }

    [Test]
    public void Merge_Empty_ReturnsEmpty()
        => KernelAccess.Merge([]).Should().BeEmpty();

    [Test]
    public void Split_AccessWithSeveralPermissions_ReturnsOneAccessPerPermission()
    {
        var split = KernelAccess.Split([Create("bins:read,write"), Create("users:write")]);

        split.Should().BeEquivalentTo([
            Create("bins:read"),
            Create("bins:write"),
            Create("users:write")
        ]);
    }

    [Test]
    public void Split_OverlappingAccesses_AreDeduplicated()
    {
        var split = KernelAccess.Split([Create("bins:read,write"), Create("bins:read")]);

        split.Should().BeEquivalentTo([Create("bins:read"), Create("bins:write")]);
    }

    [Test]
    public void Split_Empty_ReturnsEmpty()
        => KernelAccess.Split([]).Should().BeEmpty();

    [Test]
    public void MergeOfSplit_ReturnsMergedOriginal()
    {
        KernelAccess[] accesses =
        [
            Create("bins:read,write"),
            Create("bins:delete"),
            Create("users:read"),
        ];

        var roundTrip = KernelAccess.Merge(KernelAccess.Split(accesses));

        roundTrip.Should().BeEquivalentTo(KernelAccess.Merge(accesses));
    }
}
