namespace OpenKoqis.Domain.Models;

public class GeoPoint(double longitude, double latitude) : Shared.ValueObject
{
    public double Longitude { get; } = longitude is >= -180 and <= 180 ? longitude : throw new ArgumentOutOfRangeException(nameof(longitude));
    public double Latitude { get; } = latitude is >= -90 and <= 90 ? latitude : throw new ArgumentOutOfRangeException(nameof(latitude));
    protected override IEnumerable<object> GetEqualityComponents() => [Longitude, Latitude];
}

public enum BinStatus { Active, Inactive, Maintenance }
public enum BinType { Dumpster, CityBin }

public class Bin(string id, BinType type, GeoPoint location, BinStatus status) : Shared.Entity<string>(id)
{
    public BinType Type { get; } = type;
    public GeoPoint Location { get; private set; } = location;
    public BinTelemetry? Telemetry { get; private set; }
    public List<BinTelemetry> TelemetryHistory { get; } = [];
    public BinStatus Status { get; private set; } = status;

    public void UpdateTelemetry(BinTelemetry telemetry)
    {
        Telemetry = telemetry;
        TelemetryHistory.Add(telemetry);

        if (telemetry.IsSmokeDetected)
            Status = BinStatus.Maintenance;

        MarkModified();
    }

    public void ChangeStatus(BinStatus status)
    {
        Status = status;
        MarkModified();
    }
}
