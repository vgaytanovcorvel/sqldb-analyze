namespace SqlDbAnalyze.Abstractions.Models;

public record BuildPoolsRequest(
    IReadOnlyList<string> DatabaseNames,
    IReadOnlyDictionary<string, int> DtuLimits,
    double TargetPercentile = 0.99,
    double SafetyFactor = 1.10,
    int MaxDatabasesPerPool = 50,
    double MinDiversificationRatio = 1.25,
    double LowSignalP99Threshold = 5.0,
    double LowSignalStdDevThreshold = 1.5,
    double LowSignalActiveFractionThreshold = 0.05,
    int FillerMaxDatabasesPerPool = 100,
    double FillerSafetyFactor = 1.25);
