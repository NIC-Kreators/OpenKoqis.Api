using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OpenKoqis.Domain.Models;

public class ShiftLog : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public ObjectId UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public List<ObjectId> CleanedBins { get; set; } = [];
    public double DistanceTravelledKm { get; set; }
    public required string Route { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
