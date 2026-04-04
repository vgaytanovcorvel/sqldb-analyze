using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface IFillerPoolBuilder
{
    IReadOnlyList<PoolAssignment> BuildFillerPools(
        IReadOnlyList<DatabaseProfile> lowSignalProfiles,
        PoolOptimizerOptions options,
        int startPoolIndex);
}
