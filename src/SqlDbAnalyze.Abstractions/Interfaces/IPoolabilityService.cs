using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface IPoolabilityService
{
    PoolabilityMetrics ComputePairwise(
        DatabaseProfile a,
        DatabaseProfile b,
        double peakThreshold);

    IReadOnlyList<DatabaseProfile> BuildProfiles(DtuTimeSeries timeSeries);
}
