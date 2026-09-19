using OpenKoqis.Domain.Shared;

namespace OpenKoqis.Domain.Models;

public class CleaningLog(string id, string binId, string userId, int removedWeightKg, string notes) : Entity<string>(id)
{
    public string BinId { get; } = binId;
    public string UserId { get; } = userId;
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public DateTime FinishedAt { get; private set; } = DateTime.UtcNow;
    public int RemovedWeightKg { get; } = removedWeightKg;
    public string Notes { get; } = notes;
}
