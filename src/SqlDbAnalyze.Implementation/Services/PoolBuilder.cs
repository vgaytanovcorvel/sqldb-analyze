using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Implementation.Services;

public class PoolBuilder(
    IPlacementScorer placementScorer,
    IStatisticsService statisticsService) : IPoolBuilder
{
    public virtual PoolOptimizationResult BuildPools(
        IReadOnlyList<DatabaseProfile> profiles,
        PoolOptimizerOptions options)
    {
        var isolated = ExtractIsolated(profiles, options);
        var remaining = profiles
            .Where(p => !isolated.Contains(p.DatabaseName))
            .OrderByDescending(p => p.P99)
            .ToList();

        var pools = new List<MutablePool>();
        foreach (var profile in remaining)
            PlaceDatabase(profile, pools, options);

        var candidatePools = pools.Select((p, i) => ToAssignment(p, i, options)).ToList();
        var demoted = DemoteLowDiversification(candidatePools, isolated, options);

        var isolatedPools = demoted.isolated
            .Select((name, i) => BuildIsolatedPool(profiles, name, i + demoted.kept.Count, options));
        var allPools = demoted.kept.Concat(isolatedPools).ToList();

        return new PoolOptimizationResult(
            allPools,
            allPools.Sum(p => p.RecommendedCapacity),
            demoted.isolated);
    }

    private static List<string> ExtractIsolated(
        IReadOnlyList<DatabaseProfile> profiles,
        PoolOptimizerOptions options)
    {
        if (options.IsolateDatabases is null) return [];

        var profileNames = profiles.Select(p => p.DatabaseName).ToHashSet();
        return options.IsolateDatabases.Where(profileNames.Contains).ToList();
    }

    private void PlaceDatabase(
        DatabaseProfile profile,
        List<MutablePool> pools,
        PoolOptimizerOptions options)
    {
        var bestPool = FindBestPool(profile, pools, options);

        if (bestPool is not null)
        {
            bestPool.Add(profile, statisticsService);
            return;
        }

        var newPool = new MutablePool();
        newPool.Add(profile, statisticsService);
        pools.Add(newPool);
    }

    private MutablePool? FindBestPool(
        DatabaseProfile profile,
        List<MutablePool> pools,
        PoolOptimizerOptions options)
    {
        MutablePool? best = null;
        var bestScore = double.PositiveInfinity;

        for (var i = 0; i < pools.Count; i++)
        {
            if (pools[i].Profiles.Count >= options.MaxDatabasesPerPool) continue;

            var score = placementScorer.ScorePlacement(
                profile, pools[i].DatabaseNames, pools[i].Profiles,
                pools[i].CombinedLoad, i, options);

            if (score.OverloadPenalty > 0) continue;
            if (score.TotalScore >= bestScore) continue;

            bestScore = score.TotalScore;
            best = pools[i];
        }

        return best;
    }

    private PoolAssignment ToAssignment(MutablePool pool, int index, PoolOptimizerOptions options)
    {
        var capacity = statisticsService.Percentile(pool.CombinedLoad, options.TargetPercentile)
                       * options.SafetyFactor;

        if (options.MaxPoolCapacity.HasValue)
            capacity = Math.Min(capacity, options.MaxPoolCapacity.Value);

        var sumIndividualP99 = pool.Profiles.Sum(p => p.P99);
        var pooledP99 = statisticsService.Percentile(pool.CombinedLoad, 0.99);

        return new PoolAssignment(
            index,
            pool.DatabaseNames.ToList(),
            capacity,
            statisticsService.Percentile(pool.CombinedLoad, 0.95),
            pooledP99,
            pool.CombinedLoad.Count > 0 ? pool.CombinedLoad.Max() : 0,
            pooledP99 > 1e-6 ? sumIndividualP99 / pooledP99 : 1,
            statisticsService.OverloadFraction(pool.CombinedLoad, capacity));
    }

    private PoolAssignment BuildIsolatedPool(
        IReadOnlyList<DatabaseProfile> profiles,
        string dbName,
        int index,
        PoolOptimizerOptions options)
    {
        var profile = profiles.First(p => p.DatabaseName == dbName);
        var capacity = statisticsService.Percentile(profile.DtuValues, options.TargetPercentile)
                       * options.SafetyFactor;

        return new PoolAssignment(
            index, [dbName], capacity,
            statisticsService.Percentile(profile.DtuValues, 0.95),
            profile.P99,
            profile.Peak,
            1.0,
            statisticsService.OverloadFraction(profile.DtuValues, capacity));
    }

    private static (List<PoolAssignment> kept, List<string> isolated) DemoteLowDiversification(
        List<PoolAssignment> candidatePools,
        List<string> alreadyIsolated,
        PoolOptimizerOptions options)
    {
        var kept = new List<PoolAssignment>();
        var isolated = new List<string>(alreadyIsolated);

        foreach (var pool in candidatePools)
        {
            if (pool.DiversificationRatio >= options.MinDiversificationRatio)
                kept.Add(pool);
            else
                isolated.AddRange(pool.DatabaseNames);
        }

        return (kept, isolated);
    }

    private sealed class MutablePool
    {
        public List<DatabaseProfile> Profiles { get; } = [];
        public List<string> DatabaseNames { get; } = [];
        public IReadOnlyList<double> CombinedLoad { get; private set; } = [];

        public void Add(DatabaseProfile profile, IStatisticsService stats)
        {
            Profiles.Add(profile);
            DatabaseNames.Add(profile.DatabaseName);
            CombinedLoad = Profiles.Count == 1
                ? profile.DtuValues
                : stats.SumSeries(Profiles.Select(p => p.DtuValues).ToList());
        }
    }
}
