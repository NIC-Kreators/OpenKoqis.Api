using ErrorOr;
using OpenKoqis.Humans.Domain.Roles;
using OpenKoqis.Humans.Domain.Users.Errors;
using OpenKoqis.Humans.Domain.Users.Types;
using OpenKoqis.Shared.Kernel;
using OpenKoqis.Shared.Kernel.Access;

namespace OpenKoqis.Humans.Domain.Users;

/// <summary>
/// A person with an account in the system. Their effective access is the union of the
/// <see cref="DedicatedAccess"/> granted to them directly and the accesses of their <see cref="Role"/>.
/// </summary>
public class User : Entity<Guid>
{
    /// <summary>
    /// Input for <see cref="Create"/>.
    /// </summary>
    public record CreationAttributes
    {
        public required Login Login { get; init; }

        /// <summary>
        /// Id of the user in the external identity provider.
        /// </summary>
        public required string IdentityId { get; init; }

        public required HumanName Name { get; init; }
        public Guid? RoleId { get; init; }
        public IEnumerable<Access> DedicatedAccess { get; init; } = [];

        /// <summary>
        /// Contact email. Defaults to <see cref="Login"/> when the login is an <see cref="Types.Email"/>.
        /// </summary>
        public Email? Email { get; init; }

        /// <summary>
        /// Contact phone number. Defaults to <see cref="Login"/> when the login is a <see cref="Types.PhoneNumber"/>.
        /// </summary>
        public PhoneNumber? PhoneNumber { get; init; }
    }

    private HashSet<Access> _dedicatedAccess = [];

    /// <summary>
    /// The value the user signs in with.
    /// </summary>
    public required Login Login { get; init; }

    /// <summary>
    /// User's contact Email. Can be used for signing in as well.
    /// </summary>
    public Email? Email { get; private set; }

    /// <summary>
    /// User's contact Phone Number. Can be used for signing in as well.
    /// </summary>
    public PhoneNumber? PhoneNumber { get; private set; }

    /// <summary>
    /// Id of the user in the external identity provider.
    /// </summary>
    public required string IdentityId { get; init; }

    public HumanName Name { get; private set; } = null!;
    public Guid? RoleId { get; private set; }

    /// <summary>
    /// Accesses granted to this user directly, on top of the ones from the role.
    /// </summary>
    public IReadOnlySet<Access> DedicatedAccess => _dedicatedAccess;

    public Guid? LocationId { get; private set; }

    /// <summary>
    /// Whether this user is the root administrator.
    /// </summary>
    public required bool IsRoot { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user was soft-deleted; <c>null</c> while the user is active.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    private User() : base(Guid.CreateVersion7()) { }

    /// <summary>
    /// Creates a regular user, requiring a valid identity id, a non-empty role id when one
    /// is given, and at least one dedicated access. All validation errors are returned together.
    /// </summary>
    public static ErrorOr<User> Create(CreationAttributes attributes)
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
            IsRoot = false,
        };
    }

    /// <summary>
    /// Creates the root administrator. The root bypasses the access check, so it has no role
    /// and no dedicated access. Uniqueness of the root is enforced by persistence, which reports
    /// a second root as <see cref="UserErrors.RootAlreadyExists"/>.
    /// </summary>
    public static ErrorOr<User> CreateRoot(Login login, string identityId, HumanName name)
    {
        var trimmedIdentityId = identityId.Trim();

        if (trimmedIdentityId.Length is < 1 or > 2048)
            return UserErrors.InvalidIdentityId;

        return new User
        {
            Login = login,
            Email = login as Email,
            PhoneNumber = login as PhoneNumber,
            IdentityId = trimmedIdentityId,
            Name = name,
            IsRoot = true,
        };
    }

    /// <summary>
    /// Replaces the user's name with <paramref name="name"/>.
    /// </summary>
    public ErrorOr<Updated> Rename(HumanName name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;

        return Result.Updated;
    }

    /// <summary>
    /// Replaces the contact email. <c>null</c> falls back to the default of <see cref="Create"/>: the login when it is
    /// an <see cref="Types.Email"/>, otherwise no contact email. The <see cref="Login"/> and <see cref="PhoneNumber"/>
    /// are not affected.
    /// </summary>
    public ErrorOr<Updated> UpdateEmail(Email? email)
    {
        Email = email ?? Login as Email;
        UpdatedAt = DateTime.UtcNow;

        return Result.Updated;
    }

    /// <summary>
    /// Replaces the contact phone number. <c>null</c> falls back to the default of <see cref="Create"/>: the login when
    /// it is a <see cref="Types.PhoneNumber"/>, otherwise no contact phone number. The <see cref="Login"/> and
    /// <see cref="Email"/> are not affected.
    /// </summary>
    public ErrorOr<Updated> UpdatePhoneNumber(PhoneNumber? phoneNumber)
    {
        PhoneNumber = phoneNumber ?? Login as PhoneNumber;
        UpdatedAt = DateTime.UtcNow;

        return Result.Updated;
    }

    /// <summary>
    /// Soft-deletes the user by setting <see cref="DeletedAt"/>; the record is purged after the retention window.
    /// Fails with <see cref="UserErrors.RootCannotBeDeleted"/> for the root and with
    /// <see cref="UserErrors.AlreadyDeleted"/> when the user is already deleted.
    /// </summary>
    public ErrorOr<Deleted> Delete()
    {
        if (IsRoot)
            return UserErrors.RootCannotBeDeleted;

        if (DeletedAt is not null)
            return UserErrors.AlreadyDeleted;

        var now = DateTime.UtcNow;
        DeletedAt = now;
        UpdatedAt = now;

        return Result.Deleted;
    }

    /// <summary>
    /// Assigns <paramref name="newRole"/> (or clears the role when <c>null</c>)
    /// and returns the user's full access afterwards. Not allowed for the root.
    /// </summary>
    public ErrorOr<IReadOnlySet<Access>> UpdateRole(Role? newRole)
    {
        if (IsRoot)
            return UserErrors.RootAccessIsImmutable;

        RoleId = newRole?.Id;
        UpdatedAt = DateTime.UtcNow;

        return ResolveFullAccess(newRole);
    }

    /// <summary>
    /// Replaces the dedicated access with <paramref name="accesses"/> and returns
    /// the user's full access, combined with the permissions of <paramref name="currentRole"/>.
    /// Not allowed for the root.
    /// </summary>
    public ErrorOr<IReadOnlySet<Access>> UpdateDedicatedAccess(IEnumerable<Access> accesses, Role? currentRole)
    {
        if (IsRoot)
            return UserErrors.RootAccessIsImmutable;

        var accessSet = accesses.ToHashSet();

        if (accessSet.Count == 0)
            return UserErrors.AtLeastOneAccessShouldBeDefined;

        if (currentRole?.Id != RoleId)
            return UserErrors.RoleMismatch(RoleId?.ToString(), currentRole?.Id.ToString());

        _dedicatedAccess = accessSet;
        UpdatedAt = DateTime.UtcNow;

        return ResolveFullAccess(currentRole);
    }

    /// <summary>
    /// Merges the dedicated access with the accesses of <paramref name="currentRole"/>, one entry per resource.
    /// <paramref name="currentRole"/> must be the role the user is assigned to (or <c>null</c> when none is),
    /// otherwise <see cref="UserErrors.RoleMismatch"/> is returned. A user with no role resolves to an empty set.
    /// </summary>
    public ErrorOr<IReadOnlySet<Access>> ResolveFullAccess(Role? currentRole)
    {
        if (currentRole?.Id != RoleId)
            return UserErrors.RoleMismatch(RoleId?.ToString(), currentRole?.Id.ToString());

        var allPossibleAccess = _dedicatedAccess
            .Concat(currentRole?.Accesses ?? (IEnumerable<Access>)[]);

        return Access.Merge(allPossibleAccess).ToErrorOr();
    }

    public override string ToString() => Login.Value;
}
