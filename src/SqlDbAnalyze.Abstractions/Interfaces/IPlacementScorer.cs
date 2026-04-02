using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface IPlacementScorer
{
    PlacementScore ScorePlacement(
        DatabaseProfile database,
        IReadOnlyList<string> poolDatabaseNames,
        IReadOnlyList<DatabaseProfile> poolMembers,
        IReadOnlyList<double> poolCombinedLoad,
        int poolIndex,
        PoolOptimizerOptions options);
}
