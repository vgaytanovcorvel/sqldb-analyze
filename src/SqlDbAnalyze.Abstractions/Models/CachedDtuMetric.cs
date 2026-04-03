namespace SqlDbAnalyze.Abstractions.Models;

public record CachedDtuMetric(
    long CachedDtuMetricId,
    int RegisteredServerId,
    string DatabaseName,
    DateTimeOffset Timestamp,
    double DtuPercentage);
