namespace SqlDbAnalyze.Abstractions.Models;

public record BuildPoolsRequest(
    IReadOnlyList<string> DatabaseNames,
    IReadOnlyDictionary<string, int> DtuLimits,
    double TargetPercentile = 0.99,
    double SafetyFactor = 1.10,
    int MaxDatabasesPerPool = 50);
