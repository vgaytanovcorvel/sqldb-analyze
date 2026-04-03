namespace SqlDbAnalyze.Abstractions.Models;

public record PoolOptimizerOptions(
    double TargetPercentile = 0.99,
    double SafetyFactor = 1.10,
    double MaxOverloadFraction = 0.001,
    int MaxDatabasesPerPool = 50,
    double PeakThreshold = 0.90,
    double? MaxPoolCapacity = null,
    IReadOnlyList<string>? IsolateDatabases = null,
    int MaxSearchPasses = 10,
    double MinDiversificationRatio = 1.25);
