namespace OpenKoqis.Domain.Models;

public enum AlertSeverity { Info, Warning, Critical }
public enum AlertType { Smoke, Overload, Fullness, ConnectionLost }

public class Alert(string id, string binId, AlertType type, AlertSeverity severity, string message, string? valueAtTime = null) : Shared.Entity<string>(id)
{
    public string BinId { get; } = binId;
    public AlertType Type { get; } = type;
    public AlertSeverity Severity { get; } = severity;
    public string Message { get; } = message;
    public string? ValueAtTime { get; } = valueAtTime;
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
