using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface IPoolBuilder
{
    PoolOptimizationResult BuildPools(
        IReadOnlyList<DatabaseProfile> profiles,
        PoolOptimizerOptions options,
        CancellationToken cancellationToken = default);
}
