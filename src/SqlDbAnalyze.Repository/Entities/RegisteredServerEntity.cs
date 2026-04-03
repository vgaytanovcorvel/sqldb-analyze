namespace SqlDbAnalyze.Repository.Entities;

public class RegisteredServerEntity
{
    public int RegisteredServerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceGroupName { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<CachedDtuMetricEntity> CachedMetrics { get; set; } = [];
}
