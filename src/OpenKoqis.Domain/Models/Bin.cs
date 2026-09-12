namespace OpenKoqis.Domain.Models;

public record GeoPoint(double Longitude, double Latitude);

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
