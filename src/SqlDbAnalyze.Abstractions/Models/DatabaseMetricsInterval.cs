namespace SqlDbAnalyze.Abstractions.Models;

public record DatabaseMetricsInterval(
    string DatabaseName,
    DateTimeOffset? EarliestTimestamp,
    DateTimeOffset? LatestTimestamp,
    int MetricCount);
