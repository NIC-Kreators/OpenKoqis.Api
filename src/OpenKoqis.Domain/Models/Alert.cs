using OpenKoqis.Domain.Shared;


namespace OpenKoqis.Domain.Models;

public enum AlertSeverity { Info, Warning, Critical }
public enum AlertType { Smoke, Overload, Fullness, ConnectionLost }

public record AlertDetails(AlertType Type, AlertSeverity Severity, string Message, string? ValueAtTime = null);

// Fixed: ID is generated internally. Arguments are grouped into a record to avoid 3+ parameters. Inherits directly.
public class Alert(string binId, AlertDetails details) : Entity<string>(Guid.NewGuid().ToString())
{
    public string BinId { get; } = binId;
    public AlertDetails Details { get; } = details;
    public bool IsResolved { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    public void Resolve()
    {
        if (IsResolved)
            return;
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        MarkModified();
    }
}
