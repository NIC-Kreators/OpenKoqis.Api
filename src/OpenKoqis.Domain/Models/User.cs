using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OpenKoqis.Domain.Models;

public class User : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public UserRole Role { get; set; } = GuestRole.Instance;
    public required string Nickname { get; init; }
    public required string FullName { get; set; }
    public required string PasswordHash { get; set; }

    public bool PasswordRecreationRequired { get; set; } = false;
    public DateTime PasswordLastChangedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
