using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OpenKoqis.Domain.Models;

public interface IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    string Id { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
