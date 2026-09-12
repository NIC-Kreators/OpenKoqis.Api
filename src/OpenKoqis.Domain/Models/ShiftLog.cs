namespace OpenKoqis.Domain.Models;

public class ShiftLog(string id, string userId, string route) : Shared.Entity<string>(id)
{
    public string UserId { get; } = userId;
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; private set; }
    public List<string> CleanedBins { get; } = [];
    public double DistanceTravelledKm { get; private set; }
    public string Route { get; } = route;

    public void EndShift(IEnumerable<string> cleanedBins, double distance)
    {
        if (EndedAt is not null)
            return;

        EndedAt = DateTime.UtcNow;
        CleanedBins.AddRange(cleanedBins);
        DistanceTravelledKm = distance;
        MarkModified();
    }
}
