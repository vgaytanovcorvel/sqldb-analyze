namespace SqlDbAnalyze.Abstractions.Models;

public record DtuMetric(
    string DatabaseName,
    DateTimeOffset Timestamp,
    double DtuPercentage);
