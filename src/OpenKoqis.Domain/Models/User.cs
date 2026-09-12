namespace OpenKoqis.Domain.Models;

public class User(string id, string nickname, string fullName, string passwordHash, UserRole role) : Shared.Entity<string>(id)
{
    public string Nickname { get; } = nickname;
    public string FullName { get; private set; } = fullName;
    public string PasswordHash { get; private set; } = passwordHash;
    public UserRole Role { get; private set; } = role;
    public bool PasswordRecreationRequired { get; private set; }
    public DateTime PasswordLastChangedAt { get; private set; } = DateTime.UtcNow;

    public void UpdateDetails(string fullName, UserRole role)
    {
        FullName = fullName;
        Role = role;
        MarkModified();
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        PasswordRecreationRequired = false;
        PasswordLastChangedAt = DateTime.UtcNow;
        MarkModified();
    }

    public void RequirePasswordRecreation()
    {
        PasswordRecreationRequired = true;
        MarkModified();
    }
}
