namespace SqlDbAnalyze.Abstractions.Models;

public record PoolabilityMetrics(
    string DatabaseA,
    string DatabaseB,
    double PearsonCorrelation,
    double PeakCorrelation,
    double PeakOverlapFraction,
    double BadTogetherScore);
