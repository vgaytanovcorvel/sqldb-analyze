using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Implementation.Services;

public class PlacementScorer(
    IStatisticsService statisticsService,
    IPoolabilityService poolabilityService) : IPlacementScorer
{
    public virtual PlacementScore ScorePlacement(
        DatabaseProfile database,
        IReadOnlyList<string> poolDatabaseNames,
        IReadOnlyList<DatabaseProfile> poolMembers,
        IReadOnlyList<double> poolCombinedLoad,
        int poolIndex,
        PoolOptimizerOptions options)
    {
        var combinedWithNew = CombineLoads(poolCombinedLoad, database.DtuValues);
        var capacityIncrease = ComputeCapacityIncrease(poolCombinedLoad, combinedWithNew, options);
        var pairwisePenalty = ComputePairwisePenalty(database, poolMembers, options.PeakThreshold);
        var overloadPenalty = ComputeOverloadPenalty(combinedWithNew, options);

        return new PlacementScore(
            poolIndex,
            capacityIncrease,
            pairwisePenalty,
            overloadPenalty,
            capacityIncrease + pairwisePenalty * 10 + overloadPenalty);
    }

    private IReadOnlyList<double> CombineLoads(
        IReadOnlyList<double> existing,
        IReadOnlyList<double> additional)
    {
        return statisticsService.SumSeries([existing, additional]);
    }

    private double ComputeCapacityIncrease(
        IReadOnlyList<double> currentLoad,
        IReadOnlyList<double> newLoad,
        PoolOptimizerOptions options)
    {
        var currentCapacity = statisticsService.Percentile(currentLoad, options.TargetPercentile)
                              * options.SafetyFactor;
        var newCapacity = statisticsService.Percentile(newLoad, options.TargetPercentile)
                          * options.SafetyFactor;

        return newCapacity - currentCapacity;
    }

    private double ComputePairwisePenalty(
        DatabaseProfile database,
        IReadOnlyList<DatabaseProfile> poolMembers,
        double peakThreshold)
    {
        if (poolMembers.Count == 0) return 0;

        return poolMembers
            .Select(m => poolabilityService.ComputePairwise(database, m, peakThreshold).BadTogetherScore)
            .Average();
    }

    private double ComputeOverloadPenalty(
        IReadOnlyList<double> combinedLoad,
        PoolOptimizerOptions options)
    {
        var capacity = statisticsService.Percentile(combinedLoad, options.TargetPercentile)
                       * options.SafetyFactor;

        if (options.MaxPoolCapacity.HasValue)
            capacity = Math.Min(capacity, options.MaxPoolCapacity.Value);

        return statisticsService.OverloadFraction(combinedLoad, capacity) * 1_000_000;
    }
}
