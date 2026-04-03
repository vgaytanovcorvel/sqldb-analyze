using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Web.Core.Services;

public class MetricsCacheService(
    IRegisteredServerRepository registeredServerRepository,
    IMetricsCacheRepository metricsCacheRepository,
    IAzureMetricsService azureMetricsService,
    ICaptureService captureService,
    IPoolabilityService poolabilityService) : IMetricsCacheService
{
    public virtual async Task<IReadOnlyList<string>> GetDatabaseNamesAsync(
        int registeredServerId, CancellationToken cancellationToken)
    {
        var server = await registeredServerRepository.RegisteredServerSingleByIdAsync(
            registeredServerId, cancellationToken);

        return await azureMetricsService.GetDatabaseNamesAsync(
            server.SubscriptionId,
            server.ResourceGroupName,
            server.ServerName,
            cancellationToken);
    }

    public virtual async Task<IReadOnlyList<DatabaseMetricsInterval>> GetCachedIntervalsAsync(
        int registeredServerId, CancellationToken cancellationToken)
    {
        return await metricsCacheRepository.MetricsCacheGetIntervalsAsync(
            registeredServerId, cancellationToken);
    }

    public virtual async Task<DtuTimeSeries> RefreshMetricsAsync(
        int registeredServerId, int hours, CancellationToken cancellationToken)
    {
        var server = await registeredServerRepository.RegisteredServerSingleByIdAsync(
            registeredServerId, cancellationToken);

        var timeSeries = await captureService.CaptureMetricsAsync(
            server.SubscriptionId,
            server.ResourceGroupName,
            server.ServerName,
            TimeSpan.FromHours(hours),
            timeWindow: null,
            cancellationToken);

        await metricsCacheRepository.MetricsCacheUpsertAsync(
            registeredServerId, timeSeries, cancellationToken);

        return await metricsCacheRepository.MetricsCacheGetTimeSeriesAsync(
            registeredServerId, cancellationToken);
    }

    public virtual async Task<DtuTimeSeries> GetCachedTimeSeriesAsync(
        int registeredServerId, CancellationToken cancellationToken)
    {
        return await metricsCacheRepository.MetricsCacheGetTimeSeriesAsync(
            registeredServerId, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<PoolabilityMetrics>> GetCorrelationMatrixAsync(
        int registeredServerId, CancellationToken cancellationToken)
    {
        var timeSeries = await metricsCacheRepository.MetricsCacheGetTimeSeriesAsync(
            registeredServerId, cancellationToken);

        var profiles = poolabilityService.BuildProfiles(timeSeries);

        var results = new List<PoolabilityMetrics>();

        for (var i = 0; i < profiles.Count; i++)
        {
            for (var j = i + 1; j < profiles.Count; j++)
            {
                var metrics = poolabilityService.ComputePairwise(profiles[i], profiles[j], 0.80);
                results.Add(metrics);
            }
        }

        return results;
    }
}
