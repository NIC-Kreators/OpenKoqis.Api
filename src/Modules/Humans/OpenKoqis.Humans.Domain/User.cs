using ErrorOr;
using OpenKoqis.Humans.Domain.Errors;
using OpenKoqis.Shared.Kernel;

namespace OpenKoqis.Humans.Domain;

public class User : Entity<Guid>
{
    public record CreationAttributes
    {
        public required string IdentityId { get; init; }
        public required HumanName Name { get; init; }
        public Guid? RoleId { get; set; }
        public IEnumerable<Access> DedicatedAccess { get; init; } = [];
    }

    private static readonly Lock _rootAdminCreationLock = new();

    private HashSet<Access> _dedicatedAccess = [];

    public required string IdentityId { get; init; }
    public HumanName Name { get; private set; } = null!;
    public Guid? RoleId { get; private set; }
    public IReadOnlySet<Access> DedicatedAccess => _dedicatedAccess;
    public Guid? LocationId { get; private set; }
    public required bool IsRoot { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; private set; }

    private User() : base(Guid.CreateVersion7()) { }

    public static ErrorOr<User> Create(CreationAttributes attributes)
        => CreateUser(attributes);

    public static ErrorOr<User> CreateRootAdmin(CreationAttributes attributes)
    {
        if (!_rootAdminCreationLock.TryEnter())
            return UserErrors.RootAdminParallelCreation;

        return CreateUser(attributes, isRoot: true);
    }

    private static ErrorOr<User> CreateUser(CreationAttributes attributes, bool isRoot = false)
    {
        if (attributes.IdentityId.Length is < 1 or > 2048)
            return UserErrors.InvalidIdentityId;

        if (attributes.RoleId == Guid.Empty)
            return UserErrors.EmptyRoleId;

        var dedicatedAccessSet = attributes.DedicatedAccess as HashSet<Access> ?? [.. attributes.DedicatedAccess];

        if (dedicatedAccessSet.Count == 0)
            return UserErrors.AtLeastOneAccessShouldBeDefined;

        return new User
        {
            IdentityId = attributes.IdentityId,
            Name = attributes.Name,
            RoleId = attributes.RoleId,
            _dedicatedAccess = dedicatedAccessSet,
            IsRoot = isRoot
        };
    }
}
