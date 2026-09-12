namespace OpenKoqis.Domain.Models;

public class BinTelemetry(FillLevel fillLevel, bool isSmokeDetected, bool isOverloaded, DateTime? lastUpdated = null)
{
    public FillLevel FillLevel { get; } = fillLevel;
    public bool IsSmokeDetected { get; } = isSmokeDetected;
    public bool IsOverloaded { get; } = isOverloaded;
    public DateTime? LastUpdated { get; set; } = lastUpdated;
}
