namespace SqlDbAnalyze.Abstractions.Models;

public record PoolSimulationResult(
    IReadOnlyList<string> DatabaseNames,
    double P95Dtu,
    double P99Dtu,
    double PeakDtu,
    double MeanDtu,
    double DiversificationRatio,
    double OverloadFraction,
    double RecommendedPoolDtu,
    double SumIndividualDtuLimits,
    double EstimatedSavingsPercent);
