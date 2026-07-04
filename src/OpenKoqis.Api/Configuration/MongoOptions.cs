namespace OpenKoqis.Api.Options;

public class MongoOptions
{
    public required string DatabaseName { get; set; }
    public string UsersCollection { get; set; } = "Users";
    public string BinsCollection { get; set; } = "Bins";
    public string AlertsCollection { get; set; } = "Alerts";
    public string ShiftLogsCollection { get; set; } = "ShiftLogs";
    public string CleaningLogsCollection { get; set; } = "CleaningLogs";
}
