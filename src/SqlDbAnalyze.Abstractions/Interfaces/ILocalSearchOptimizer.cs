using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface ILocalSearchOptimizer
{
    PoolOptimizationResult Improve(
        PoolOptimizationResult initial,
        IReadOnlyList<DatabaseProfile> profiles,
        PoolOptimizerOptions options);
}
