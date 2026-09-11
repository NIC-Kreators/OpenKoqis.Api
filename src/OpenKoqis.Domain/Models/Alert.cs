using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OpenKoqis.Domain.Models;

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

public enum AlertType
{
    Smoke,
    Overload,
    Fullness,
    ConnectionLost
}

public class Alert : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string BinId { get; set; } = null!;

    public AlertType Type { get; set; }

    public AlertSeverity Severity { get; set; }

    public string Message { get; set; } = null!; // Message description

    public string? ValueAtTime { get; set; } // Detector value when anomaly occured


    public bool IsResolved { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }
}
