namespace SqlDbAnalyze.Repository.Entities;

public class CachedDtuMetricEntity
{
    public long CachedDtuMetricId { get; set; }
    public int RegisteredServerId { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public double DtuPercentage { get; set; }
    public RegisteredServerEntity RegisteredServer { get; set; } = null!;
}
