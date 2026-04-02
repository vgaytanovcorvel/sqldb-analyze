using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Implementation.Services;

public class LocalSearchOptimizer(IStatisticsService statisticsService) : ILocalSearchOptimizer
{
    public virtual PoolOptimizationResult Improve(
        PoolOptimizationResult initial,
        IReadOnlyList<DatabaseProfile> profiles,
        PoolOptimizerOptions options)
    {
        var current = initial;

        for (var pass = 0; pass < options.MaxSearchPasses; pass++)
        {
            var improved = RunSinglePass(current, profiles, options);
            if (improved is null) break;
            current = improved;
        }

        return current;
    }

    private PoolOptimizationResult? RunSinglePass(
        PoolOptimizationResult current,
        IReadOnlyList<DatabaseProfile> profiles,
        PoolOptimizerOptions options)
    {
        var profileMap = profiles.ToDictionary(p => p.DatabaseName);

        foreach (var sourcePool in current.Pools)
        {
            var move = FindBestMove(sourcePool, current.Pools, profileMap, options);
            if (move is not null)
                return ApplyMove(current, move.Value, profileMap, options);
        }

        return null;
    }

    private MoveCandidate? FindBestMove(
        PoolAssignment sourcePool,
        IReadOnlyList<PoolAssignment> allPools,
        Dictionary<string, DatabaseProfile> profileMap,
        PoolOptimizerOptions options)
    {
        MoveCandidate? best = null;

        foreach (var dbName in sourcePool.DatabaseNames)
        {
            var candidate = FindBestTarget(dbName, sourcePool, allPools, profileMap, options);
            if (candidate is not null && (best is null || candidate.Value.Saving > best.Value.Saving))
                best = candidate;
        }

        return best;
    }

    private MoveCandidate? FindBestTarget(
        string dbName,
        PoolAssignment sourcePool,
        IReadOnlyList<PoolAssignment> allPools,
        Dictionary<string, DatabaseProfile> profileMap,
        PoolOptimizerOptions options)
    {
        MoveCandidate? best = null;

        foreach (var targetPool in allPools)
        {
            if (targetPool.PoolIndex == sourcePool.PoolIndex) continue;
            if (targetPool.DatabaseNames.Count >= options.MaxDatabasesPerPool) continue;

            var saving = EvaluateMove(dbName, sourcePool, targetPool, profileMap, options);
            if (saving > 1e-6 && (best is null || saving > best.Value.Saving))
                best = new MoveCandidate(dbName, sourcePool.PoolIndex, targetPool.PoolIndex, saving);
        }

        return best;
    }

    private double EvaluateMove(
        string dbName,
        PoolAssignment sourcePool,
        PoolAssignment targetPool,
        Dictionary<string, DatabaseProfile> profileMap,
        PoolOptimizerOptions options)
    {
        var sourceWithout = ComputePoolCapacity(
            sourcePool.DatabaseNames.Where(n => n != dbName).ToList(), profileMap, options);
        var targetWith = ComputePoolCapacity(
            targetPool.DatabaseNames.Append(dbName).ToList(), profileMap, options);

        var before = sourcePool.RecommendedCapacity + targetPool.RecommendedCapacity;
        var after = sourceWithout + targetWith;

        return before - after;
    }

    private double ComputePoolCapacity(
        List<string> dbNames,
        Dictionary<string, DatabaseProfile> profileMap,
        PoolOptimizerOptions options)
    {
        if (dbNames.Count == 0) return 0;

        var series = dbNames.Select(n => profileMap[n].DtuValues).ToList();
        var combined = statisticsService.SumSeries(series);

        return statisticsService.Percentile(combined, options.TargetPercentile) * options.SafetyFactor;
    }

    private PoolOptimizationResult ApplyMove(
        PoolOptimizationResult current,
        MoveCandidate move,
        Dictionary<string, DatabaseProfile> profileMap,
        PoolOptimizerOptions options)
    {
        var newPools = current.Pools
            .Select(p => RebuildPool(p, move, profileMap, options))
            .Where(p => p.DatabaseNames.Count > 0)
            .Select((p, i) => p with { PoolIndex = i })
            .ToList();

        return current with
        {
            Pools = newPools,
            TotalRequiredCapacity = newPools.Sum(p => p.RecommendedCapacity)
        };
    }

    private PoolAssignment RebuildPool(
        PoolAssignment pool,
        MoveCandidate move,
        Dictionary<string, DatabaseProfile> profileMap,
        PoolOptimizerOptions options)
    {
        List<string> newNames;

        if (pool.PoolIndex == move.SourcePoolIndex)
            newNames = pool.DatabaseNames.Where(n => n != move.DatabaseName).ToList();
        else if (pool.PoolIndex == move.TargetPoolIndex)
            newNames = pool.DatabaseNames.Append(move.DatabaseName).ToList();
        else
            return pool;

        return BuildAssignment(newNames, pool.PoolIndex, profileMap, options);
    }

    private PoolAssignment BuildAssignment(
        List<string> dbNames,
        int poolIndex,
        Dictionary<string, DatabaseProfile> profileMap,
        PoolOptimizerOptions options)
    {
        if (dbNames.Count == 0)
            return new PoolAssignment(poolIndex, [], 0, 0, 0, 0, 0, 0);

        var series = dbNames.Select(n => profileMap[n].DtuValues).ToList();
        var combined = statisticsService.SumSeries(series);

        var capacity = statisticsService.Percentile(combined, options.TargetPercentile) * options.SafetyFactor;
        if (options.MaxPoolCapacity.HasValue)
            capacity = Math.Min(capacity, options.MaxPoolCapacity.Value);

        var sumP99 = dbNames.Sum(n => profileMap[n].P99);
        var pooledP99 = statisticsService.Percentile(combined, 0.99);

        return new PoolAssignment(
            poolIndex, dbNames, capacity,
            statisticsService.Percentile(combined, 0.95),
            pooledP99,
            combined.Max(),
            pooledP99 > 1e-6 ? sumP99 / pooledP99 : 1,
            statisticsService.OverloadFraction(combined, capacity));
    }

    private readonly record struct MoveCandidate(
        string DatabaseName,
        int SourcePoolIndex,
        int TargetPoolIndex,
        double Saving);
}
