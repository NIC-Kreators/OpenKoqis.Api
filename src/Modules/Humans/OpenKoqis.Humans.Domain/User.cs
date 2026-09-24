using ErrorOr;
using OpenKoqis.Humans.Domain.Errors;
using OpenKoqis.Shared.Kernel;

namespace OpenKoqis.Humans.Domain;

public class User : Entity<Guid>
{
    public record CreationAttributes
    {
        public required Login Login { get; init; }
        public required string IdentityId { get; init; }
        public required HumanName Name { get; init; }
        public Guid? RoleId { get; init; }
        public IEnumerable<Access> DedicatedAccess { get; init; } = [];

        public Email? Email { get; init; }
        public PhoneNumber? PhoneNumber { get; init; }
    }

    private static readonly Lock _rootAdminCreationLock = new();

    private HashSet<Access> _dedicatedAccess = [];

    public required Login Login { get; init; }
    public Email? Email { get; init; }
    public PhoneNumber? PhoneNumber { get; set; }

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
        var trimmedIdentityId = attributes.IdentityId.Trim();

        List<Error> errors = [];

        var login = attributes.Login;
        var email = attributes.Email;
        var phoneNumber = attributes.PhoneNumber;

        if (login is Email loginEmail)
            email ??= loginEmail;

        if (login is PhoneNumber loginPhoneNumber)
            phoneNumber ??= loginPhoneNumber;

        if (trimmedIdentityId.Length is < 1 or > 2048)
            errors.Add(UserErrors.InvalidIdentityId);

        if (attributes.RoleId == Guid.Empty)
            errors.Add(UserErrors.EmptyRoleId);

        var dedicatedAccessSet = attributes.DedicatedAccess.ToHashSet();

        if (dedicatedAccessSet.Count == 0)
            errors.Add(UserErrors.AtLeastOneAccessShouldBeDefined);

        if (errors.Count > 0)
            return errors;

        return new User
        {
            Login = attributes.Login,
            Email = email,
            PhoneNumber = phoneNumber,
            IdentityId = trimmedIdentityId,
            Name = attributes.Name,
            RoleId = attributes.RoleId,
            _dedicatedAccess = dedicatedAccessSet,
            IsRoot = isRoot
        };
    }

    public ErrorOr<Updated> Rename(HumanName name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;

        return Result.Updated;
    }

    /// <summary>
    /// Assigns <paramref name="newRole"/> (or clears the role when <c>null</c>)
    /// and returns the user's full access afterwards.
    /// </summary>
    public ErrorOr<IReadOnlySet<Access>> UpdateRole(Role? newRole)
    {
        RoleId = newRole?.Id;
        UpdatedAt = DateTime.UtcNow;

        return ResolveFullAccess(newRole);
    }

    /// <summary>
    /// Replaces the dedicated access with <paramref name="accesses"/> and returns
    /// the user's full access, combined with the permissions of <paramref name="currentRole"/>.
    /// </summary>
    public ErrorOr<IReadOnlySet<Access>> UpdateDedicatedAccess(IEnumerable<Access> accesses, Role? currentRole)
    {
        var accessSet = accesses.ToHashSet();

        if (accessSet.Count == 0)
            return UserErrors.AtLeastOneAccessShouldBeDefined;

        if (currentRole?.Id != RoleId)
            return UserErrors.RoleMismatch(RoleId?.ToString(), currentRole?.Id.ToString());

        _dedicatedAccess = accessSet;
        UpdatedAt = DateTime.UtcNow;

        return ResolveFullAccess(currentRole);
    }

    public ErrorOr<IReadOnlySet<Access>> ResolveFullAccess(Role? currentRole)
    {
        if (currentRole?.Id != RoleId)
            return UserErrors.RoleMismatch(RoleId?.ToString(), currentRole?.Id.ToString());

        if (currentRole is null && !RoleId.HasValue)
            return [];

        var allPossibleAccess = _dedicatedAccess
            .Concat(currentRole?.Accesses ?? (IEnumerable<Access>)[]);

        return Access.Merge(allPossibleAccess).ToErrorOr();
    }
}
