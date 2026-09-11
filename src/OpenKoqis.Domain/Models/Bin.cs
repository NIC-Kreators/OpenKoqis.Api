using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OpenKoqis.Domain.Models;

public struct GeoPoint
{
    [BsonElement("type")]
    public string Type { get; set; } = "Point";

    [BsonElement("coordinates")]
    public double[] Coordinates { get; set; } // [longitude, latitude]

    public GeoPoint(double[] coordinates)
    {
        if (coordinates.Length != 2)
            throw new ArgumentException("Coordinates must contain exactly two elements: [longitude, latitude].");

        Coordinates = coordinates;
    }
}

public enum BinStatus
{
    Active,
    Inactive,
    Maintenance,
}

public enum BinType
{
    Dumpster,
    CityBin,
}

public class Bin : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public BinType Type { get; set; }
    public GeoPoint Location { get; set; }
    public required BinTelemetry Telemetry { get; set; }
    public BinTelemetry[] TelemetryHistory { get; set; } = [];
    public BinStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
