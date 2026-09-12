namespace OpenKoqis.Domain.Models;

public abstract record UserRole
{
    public abstract string Name { get; }
    public virtual List<string> Permissions => [];

    public abstract bool HasPermissionsOf(UserRole otherRole);

    public static UserRole Parse(string roleName) => roleName switch
    {
        "Admin" => AdminRole.Instance,
        "SalesManager" => SalesManagerRole.Instance,
        "Guest" => GuestRole.Instance,
        _ => throw new ArgumentException($"Unknown role: {roleName}")
    };
}

public record AdminRole : UserRole
{
    public static readonly AdminRole Instance = new();
    public override string Name => "Admin";
    public override bool HasPermissionsOf(UserRole otherRole) => true;
}

public record SalesManagerRole : UserRole
{
    public static readonly SalesManagerRole Instance = new();
    public override string Name => "SalesManager";
    public override bool HasPermissionsOf(UserRole otherRole) => otherRole is not AdminRole;
}

public record GuestRole : UserRole
{
    public static readonly GuestRole Instance = new();
    public override string Name => "Guest";
    public override bool HasPermissionsOf(UserRole otherRole) => otherRole is GuestRole;
}
