using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Implementation.Services;

public class FillerPoolBuilder(IStatisticsService statisticsService) : IFillerPoolBuilder
{
    public virtual IReadOnlyList<PoolAssignment> BuildFillerPools(
        IReadOnlyList<DatabaseProfile> lowSignalProfiles,
        PoolOptimizerOptions options,
        int startPoolIndex)
    {
        if (lowSignalProfiles.Count == 0) return [];

        var sorted = lowSignalProfiles
            .OrderByDescending(p => p.Peak)
            .ThenByDescending(p => p.ActiveFraction)
            .ToList();

        var pools = PackIntoFillerPools(sorted, options);
        return pools
            .Select((pool, i) => BuildFillerAssignment(pool, startPoolIndex + i, options))
            .ToList();
    }

    private static List<List<DatabaseProfile>> PackIntoFillerPools(
        List<DatabaseProfile> sorted,
        PoolOptimizerOptions options)
    {
        var pools = new List<List<DatabaseProfile>>();
        var currentPool = new List<DatabaseProfile>();

        foreach (var profile in sorted)
        {
            if (currentPool.Count >= options.FillerMaxDatabasesPerPool)
            {
                pools.Add(currentPool);
                currentPool = [];
            }

            currentPool.Add(profile);
        }

        if (currentPool.Count > 0)
            pools.Add(currentPool);

        return pools;
    }

    private PoolAssignment BuildFillerAssignment(
        List<DatabaseProfile> profiles,
        int poolIndex,
        PoolOptimizerOptions options)
    {
        var flooredSeries = profiles.Select(p => ApplyFloorSeries(p, options)).ToList();
        var combined = statisticsService.SumSeries(flooredSeries);
        var sorted = SortValues(combined);

        var capacity = statisticsService.PercentilePreSorted(sorted, options.TargetPercentile)
                       * options.FillerSafetyFactor;

        if (options.MaxPoolCapacity.HasValue)
            capacity = Math.Min(capacity, options.MaxPoolCapacity.Value);

        var p95 = statisticsService.PercentilePreSorted(sorted, 0.95);
        var p99 = statisticsService.PercentilePreSorted(sorted, 0.99);
        var peak = combined.Count > 0 ? combined.Max() : 0;
        var sumP99 = profiles.Sum(p => p.P99);
        var diversification = p99 > 1e-6 ? sumP99 / p99 : 1;
        var overload = statisticsService.OverloadFraction(combined, capacity);

        return new PoolAssignment(
            poolIndex,
            profiles.Select(p => p.DatabaseName).ToList(),
            capacity, p95, p99, peak,
            diversification, overload,
            IsFillerPool: true);
    }

    private static IReadOnlyList<double> ApplyFloorSeries(
        DatabaseProfile profile,
        PoolOptimizerOptions options)
    {
        var floor = ComputeFloorDtu(profile.Peak, options);
        var values = new double[profile.DtuValues.Count];

        for (var i = 0; i < profile.DtuValues.Count; i++)
            values[i] = Math.Max(profile.DtuValues[i], floor);

        return values;
    }

    private static double ComputeFloorDtu(double peak, PoolOptimizerOptions options)
    {
        var floor = Math.Max(options.FillerFloorDtuMin, options.FillerFloorDtuFactor * peak);
        return Math.Min(floor, options.FillerFloorDtuCap);
    }

    private static double[] SortValues(IReadOnlyList<double> values)
    {
        var sorted = new double[values.Count];
        for (var i = 0; i < values.Count; i++)
            sorted[i] = values[i];
        Array.Sort(sorted);
        return sorted;
    }
}
