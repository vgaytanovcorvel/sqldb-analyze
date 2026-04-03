using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface IMetricsCacheRepository
{
    Task<IReadOnlyList<DatabaseMetricsInterval>> MetricsCacheGetIntervalsAsync(
        int registeredServerId,
        CancellationToken cancellationToken);

    Task<DtuTimeSeries> MetricsCacheGetTimeSeriesAsync(
        int registeredServerId,
        CancellationToken cancellationToken);

    Task MetricsCacheUpsertAsync(
        int registeredServerId,
        DtuTimeSeries timeSeries,
        CancellationToken cancellationToken);

    Task MetricsCacheDeleteByServerAsync(
        int registeredServerId,
        CancellationToken cancellationToken);
}
