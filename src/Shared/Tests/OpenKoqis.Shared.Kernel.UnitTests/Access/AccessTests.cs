using AwesomeAssertions;
using ErrorOr;
using KernelAccess = OpenKoqis.Shared.Kernel.Access.Access;

namespace OpenKoqis.Shared.Kernel.UnitTests.Access;

public class AccessTests
{
    private static KernelAccess Create(string raw) => KernelAccess.Parse(raw).Value;

    /// <summary>
    /// An access string with one permission parses into that resource and permission.
    /// </summary>
    [Test]
    public void Parse_SinglePermission_ReturnsAccess()
    {
        // Act
        var result = KernelAccess.Parse("bins:read");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Resource.Should().Be("bins");
        result.Value.Permissions.Should().BeEquivalentTo("read");
    }

    /// <summary>
    /// Every comma-separated permission ends up in <see cref="KernelAccess.Permissions"/>.
    /// </summary>
    [Test]
    public void Parse_SeveralPermissions_ReturnsAllOfThem()
    {
        // Act
        var result = KernelAccess.Parse("bins:read,write,delete");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Permissions.Should().BeEquivalentTo("read", "write", "delete");
    }

    /// <summary>
    /// The resource and the permissions are stored lowercased and trimmed.
    /// </summary>
    [Test]
    public void Parse_MixedCaseAndWhitespace_NormalizesResourceAndPermissions()
    {
        // Act
        var result = KernelAccess.Parse(" Bins : Read , WRITE ");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Resource.Should().Be("bins");
        result.Value.Permissions.Should().BeEquivalentTo("read", "write");
    }

    /// <summary>
    /// Empty and whitespace-only entries between commas are skipped rather than rejected.
    /// </summary>
    [Test]
    public void Parse_EmptyPermissionEntries_AreIgnored()
    {
        // Act
        var result = KernelAccess.Parse("bins:read,, ,write");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Permissions.Should().BeEquivalentTo("read", "write");
    }

    /// <summary>
    /// Permissions that are equal after normalization are stored once.
    /// </summary>
    [Test]
    public void Parse_DuplicatePermissions_AreDeduplicated()
    {
        // Act
        var result = KernelAccess.Parse("bins:read,READ, read");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Permissions.Should().BeEquivalentTo("read");
    }

    /// <summary>
    /// A string that is not exactly <c>resource:permissions</c> with a non-blank resource and at least one
    /// permission returns a single <c>Access.InvalidAccessString</c> validation error quoting the input.
    /// </summary>
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
        // Act
        var result = KernelAccess.Parse(raw);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.FirstError.Code.Should().Be("Access.InvalidAccessString");
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().EndWith($"Received: {raw}");
    }

    /// <summary>
    /// A resource that breaks the system-name rules returns a single <c>Access.InvalidResource</c>
    /// validation error quoting the resource.
    /// </summary>
    [Test]
    public void Parse_InvalidResource_ReturnsInvalidResource()
    {
        // Act
        var result = KernelAccess.Parse("bin_s:read");

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.FirstError.Code.Should().Be("Access.InvalidResource");
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().EndWith("Received: bin_s");
    }

    /// <summary>
    /// A permission that breaks the system-name rules returns a single <c>Access.InvalidPermission</c>
    /// validation error quoting the normalized permission.
    /// </summary>
    [Test]
    public void Parse_InvalidPermission_ReturnsInvalidPermission()
    {
        // Act
        var result = KernelAccess.Parse("bins:read,Read_Only");

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.FirstError.Code.Should().Be("Access.InvalidPermission");
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().EndWith("Received: read_only");
    }

    /// <summary>
    /// Every invalid permission and an invalid resource are all reported together, one error each.
    /// </summary>
    [Test]
    public void Parse_SeveralInvalidNames_ReturnsErrorForEach()
    {
        // Act
        var result = KernelAccess.Parse("bin_s:re_ad,wr_ite");

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().BeEquivalentTo(
            "Access.InvalidPermission", "Access.InvalidPermission", "Access.InvalidResource");
    }

    /// <summary>
    /// A resource name of exactly 32 characters, the maximum, is accepted.
    /// </summary>
    [Test]
    public void Parse_ResourceOf32Characters_ReturnsAccess()
    {
        // Arrange
        var resource = new string('a', 32);

        // Act
        var result = KernelAccess.Parse($"{resource}:read");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Resource.Should().Be(resource);
    }

    /// <summary>
    /// A resource name one character over the 32-character maximum returns <c>Access.InvalidResource</c>.
    /// </summary>
    [Test]
    public void Parse_ResourceOf33Characters_ReturnsInvalidResource()
    {
        // Arrange
        var resource = new string('a', 33);

        // Act
        var result = KernelAccess.Parse($"{resource}:read");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Access.InvalidResource");
    }

    /// <summary>
    /// A permission over the default 16-character maximum returns <c>Access.InvalidPermission</c>.
    /// </summary>
    [Test]
    public void Parse_PermissionOver16Characters_ReturnsInvalidPermission()
    {
        // Arrange
        var permission = new string('a', 17);

        // Act
        var result = KernelAccess.Parse($"bins:{permission}");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Access.InvalidPermission");
    }

    /// <summary>
    /// The overload taking a separate resource and permissions normalizes both the same way as the string overload.
    /// </summary>
    [Test]
    public void ParseSeparated_ValidInput_ReturnsNormalizedAccess()
    {
        // Act
        var result = KernelAccess.Parse(" Bins ", [" Read", "WRITE "]);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Resource.Should().Be("bins");
        result.Value.Permissions.Should().BeEquivalentTo(["read", "write"]);
    }

    /// <summary>
    /// An empty permission list returns a single <c>Access.NoPermissions</c> validation error.
    /// </summary>
    [Test]
    public void ParseSeparated_NoPermissions_ReturnsNoPermissions()
    {
        // Act
        var result = KernelAccess.Parse("bins", []);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.FirstError.Code.Should().Be("Access.NoPermissions");
        result.FirstError.Type.Should().Be(ErrorType.Validation);
    }

    /// <summary>
    /// Unlike the string overload, a blank permission is not skipped: it returns <c>Access.InvalidPermission</c>.
    /// </summary>
    [Test]
    public void ParseSeparated_EmptyPermission_ReturnsInvalidPermission()
    {
        // Act
        var result = KernelAccess.Parse("bins", ["read", " "]);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.FirstError.Code.Should().Be("Access.InvalidPermission");
    }

    /// <summary>
    /// An empty resource with no permissions reports both <c>Access.NoPermissions</c> and <c>Access.InvalidResource</c>.
    /// </summary>
    [Test]
    public void ParseSeparated_EmptyResourceAndNoPermissions_ReturnsBothErrors()
    {
        // Act
        var result = KernelAccess.Parse("", []);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().BeEquivalentTo("Access.NoPermissions", "Access.InvalidResource");
    }

    /// <summary>
    /// Accesses are equal, and hash the same, when their resource and permission sets match,
    /// whatever order the permissions were given in.
    /// </summary>
    [Test]
    public void Equals_SamePermissionsInDifferentOrder_ReturnsTrue()
    {
        // Arrange
        var left = Create("bins:read,write");
        var right = Create("bins:write,read");

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
    /// Accesses with a different resource, fewer permissions or more permissions are not equal.
    /// </summary>
    [Test]
    [Arguments("bins:read")]
    [Arguments("bins:read,write,delete")]
    [Arguments("users:read,write")]
    public void Equals_DifferentResourceOrPermissions_ReturnsFalse(string other)
    {
        // Arrange
        var access = Create("bins:read,write");
        var otherAccess = Create(other);

        // Act
        var equalityOperator = access == otherAccess;

        // Assert
        access.Should().NotBe(otherAccess);
        equalityOperator.Should().BeFalse();
    }

    /// <summary>
    /// <see cref="KernelAccess.Merge"/> produces one access per resource, uniting the permissions of all accesses on it.
    /// </summary>
    [Test]
    public void Merge_AccessesOnSameResource_UnitesTheirPermissions()
    {
        // Arrange
        KernelAccess[] accesses = [Create("bins:read"), Create("bins:write"), Create("users:write")];

        // Act
        var merged = KernelAccess.Merge(accesses);

        // Assert
        merged.Should().BeEquivalentTo([
            Create("bins:read,write"),
            Create("users:write")
        ]);
    }

    /// <summary>
    /// Permissions present in several merged accesses appear once in the result.
    /// </summary>
    [Test]
    public void Merge_OverlappingPermissions_AreDeduplicated()
    {
        // Arrange
        KernelAccess[] accesses = [Create("bins:read,write"), Create("bins:write,delete")];

        // Act
        var merged = KernelAccess.Merge(accesses);

        // Assert
        merged.Should().ContainSingle()
            .Which.Permissions.Should().BeEquivalentTo("read", "write", "delete");
    }

    /// <summary>
    /// Merging nothing returns an empty set.
    /// </summary>
    [Test]
    public void Merge_Empty_ReturnsEmpty()
    {
        // Act
        var merged = KernelAccess.Merge([]);

        // Assert
        merged.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="KernelAccess.Split"/> produces one single-permission access per resource–permission pair.
    /// </summary>
    [Test]
    public void Split_AccessWithSeveralPermissions_ReturnsOneAccessPerPermission()
    {
        // Arrange
        KernelAccess[] accesses = [Create("bins:read,write"), Create("users:write")];

        // Act
        var split = KernelAccess.Split(accesses);

        // Assert
        split.Should().BeEquivalentTo([
            Create("bins:read"),
            Create("bins:write"),
            Create("users:write")
        ]);
    }

    /// <summary>
    /// A resource–permission pair present in several accesses appears once in the split result.
    /// </summary>
    [Test]
    public void Split_OverlappingAccesses_AreDeduplicated()
    {
        // Arrange
        KernelAccess[] accesses = [Create("bins:read,write"), Create("bins:read")];

        // Act
        var split = KernelAccess.Split(accesses);

        // Assert
        split.Should().BeEquivalentTo([Create("bins:read"), Create("bins:write")]);
    }

    /// <summary>
    /// Splitting nothing returns an empty set.
    /// </summary>
    [Test]
    public void Split_Empty_ReturnsEmpty()
    {
        // Act
        var split = KernelAccess.Split([]);

        // Assert
        split.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="KernelAccess.Merge"/> undoes <see cref="KernelAccess.Split"/>: merging the split accesses
    /// gives the same result as merging the originals.
    /// </summary>
    [Test]
    public void MergeOfSplit_ReturnsMergedOriginal()
    {
        // Arrange
        KernelAccess[] accesses =
        [
            Create("bins:read,write"),
            Create("bins:delete"),
            Create("users:read"),
        ];

        // Act
        var roundTrip = KernelAccess.Merge(KernelAccess.Split(accesses));

        // Assert
        roundTrip.Should().BeEquivalentTo(KernelAccess.Merge(accesses));
    }
}
