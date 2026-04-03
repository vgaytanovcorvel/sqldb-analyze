using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface IMetricsCacheService
{
    Task<IReadOnlyList<string>> GetDatabaseNamesAsync(int registeredServerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DatabaseMetricsInterval>> GetCachedIntervalsAsync(
        int registeredServerId,
        CancellationToken cancellationToken);

    Task<DtuTimeSeries> RefreshMetricsAsync(
        int registeredServerId,
        int hours,
        CancellationToken cancellationToken);

    Task<DtuTimeSeries> GetCachedTimeSeriesAsync(
        int registeredServerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PoolabilityMetrics>> GetCorrelationMatrixAsync(
        int registeredServerId,
        CancellationToken cancellationToken);
}
