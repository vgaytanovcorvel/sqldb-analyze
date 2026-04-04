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
    double MinDiversificationRatio = 1.25,
    double LowSignalP99Threshold = 5.0,
    double LowSignalStdDevThreshold = 1.5,
    double LowSignalActiveFractionThreshold = 0.05,
    double LowSignalActiveValueThreshold = 1.5,
    double FillerFloorDtuFactor = 0.25,
    double FillerFloorDtuCap = 5.0,
    double FillerFloorDtuMin = 1.0,
    int FillerMaxDatabasesPerPool = 100,
    double FillerSafetyFactor = 1.25);
