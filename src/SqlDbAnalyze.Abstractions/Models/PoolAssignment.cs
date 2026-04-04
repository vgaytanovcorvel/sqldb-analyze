namespace SqlDbAnalyze.Abstractions.Models;

public record PoolAssignment(
    int PoolIndex,
    IReadOnlyList<string> DatabaseNames,
    double RecommendedCapacity,
    double P95Load,
    double P99Load,
    double PeakLoad,
    double DiversificationRatio,
    double OverloadFraction,
    bool IsFillerPool = false);
