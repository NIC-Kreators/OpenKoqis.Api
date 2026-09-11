using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OpenKoqis.Domain.Models;

public class CleaningLog : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public ObjectId BinId { get; set; }
    public ObjectId UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public int RemovedWeightKg { get; set; }
    public required string Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
