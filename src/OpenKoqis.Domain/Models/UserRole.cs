namespace OpenKoqis.Domain.Models;

// Base record for all roles. Must be abstract.
public abstract record UserRole
{
    // Unique string identifier to be used in JWT Claims.
    public abstract string Name { get; }

    // List of permissions associated with this role.
    // Can be used for complex authorization beyond simple hierarchy.
    public virtual List<string> Permissions =>
        [];

    // Method to compare roles (equivalent to the "comparison" logic).
    // Checks if the current role possesses at least the permissions of the other role.
    public abstract bool HasPermissionsOf(UserRole otherRole);

    // Static method to parse a string from Claims back into a Record object.
    public static UserRole Parse(string roleName)
    {
        return roleName switch
        {
            "Admin" => AdminRole.Instance,
            "SalesManager" => SalesManagerRole.Instance,
            "Guest" => GuestRole.Instance,
            _ => throw new ArgumentException($"Unknown role: {roleName}")
        };
    }
}

// --- Concrete Roles (Singletons for comparison) ---

public record AdminRole : UserRole
{
    public static readonly AdminRole Instance = new();

    public override string Name =>
        "Admin";

    // Admin possesses the permissions of all other roles.
    public override bool HasPermissionsOf(UserRole otherRole) =>
        true;
}

public record SalesManagerRole : UserRole
{
    public static readonly SalesManagerRole Instance = new();

    public override string Name =>
        "SalesManager";

    // SalesManager possesses Guest permissions.
    public override bool HasPermissionsOf(UserRole otherRole) =>
        otherRole is not AdminRole; // Possesses permissions of everyone except Admin.
}

public record GuestRole : UserRole
{
    public static readonly GuestRole Instance = new();

    public override string Name =>
        "Guest";

    // Guest possesses only its own permissions.
    public override bool HasPermissionsOf(UserRole otherRole) =>
        otherRole is GuestRole;
}
