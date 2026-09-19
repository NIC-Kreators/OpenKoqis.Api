using OpenKoqis.Domain.Shared;

namespace OpenKoqis.Domain.Models;

public class User(string id, Username nickname, FullName fullName, PasswordHash passwordHash, UserRole role) : Entity<string>(id)
{
    public Username Nickname { get; } = nickname;
    public FullName FullName { get; private set; } = fullName;
    public PasswordHash PasswordHash { get; private set; } = passwordHash;
    public UserRole Role { get; private set; } = role;
    public bool PasswordRecreationRequired { get; private set; }
    public DateTime PasswordLastChangedAt { get; private set; } = DateTime.UtcNow;

    public void UpdateDetails(FullName fullName, UserRole role)
    {
        FullName = fullName;
        Role = role;
        MarkModified();
    }

    public void ChangePassword(PasswordHash newPasswordHash)
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
