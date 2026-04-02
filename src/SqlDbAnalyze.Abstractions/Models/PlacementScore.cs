namespace SqlDbAnalyze.Abstractions.Models;

public record PlacementScore(
    int PoolIndex,
    double CapacityIncrease,
    double PairwisePenalty,
    double OverloadPenalty,
    double TotalScore);
