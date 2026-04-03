using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Web.Core.Services;

public class MetricsCacheService(
    IRegisteredServerRepository registeredServerRepository,
    IMetricsCacheRepository metricsCacheRepository,
    IAzureMetricsService azureMetricsService,
    ICaptureService captureService,
    IPoolabilityService poolabilityService,
    IStatisticsService statisticsService,
    IPoolBuilder poolBuilder,
    ILocalSearchOptimizer localSearchOptimizer) : IMetricsCacheService
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

    public virtual async Task<IReadOnlyList<DatabaseInfo>> GetDatabaseInfoAsync(
        int registeredServerId, CancellationToken cancellationToken)
    {
        var server = await registeredServerRepository.RegisteredServerSingleByIdAsync(
            registeredServerId, cancellationToken);

        return await azureMetricsService.GetDatabaseInfoAsync(
            server.SubscriptionId,
            server.ResourceGroupName,
            server.ServerName,
            cancellationToken);
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

    public virtual async Task<PoolSimulationResult> SimulatePoolAsync(
        int registeredServerId,
        PoolSimulationRequest request,
        CancellationToken cancellationToken)
    {
        var timeSeries = await metricsCacheRepository.MetricsCacheGetTimeSeriesAsync(
            registeredServerId, cancellationToken);

        var dtuSeries = ConvertToAbsoluteDtu(timeSeries, request);
        var combinedLoad = statisticsService.SumSeries(dtuSeries);

        var sumIndividualLimits = request.DatabaseNames
            .Where(request.DtuLimits.ContainsKey)
            .Sum(name => request.DtuLimits[name]);

        var p95 = statisticsService.Percentile(combinedLoad, 0.95);
        var p99 = statisticsService.Percentile(combinedLoad, 0.99);
        var peak = combinedLoad.Count > 0 ? combinedLoad.Max() : 0;
        var mean = statisticsService.Mean(combinedLoad);

        const double safetyFactor = 1.10;
        var recommendedDtu = p99 * safetyFactor;

        var diversificationRatio = sumIndividualLimits > 0 ? (double)sumIndividualLimits / recommendedDtu : 1;
        var overloadFraction = statisticsService.OverloadFraction(combinedLoad, recommendedDtu);

        var savingsPercent = sumIndividualLimits > 0
            ? (1 - recommendedDtu / sumIndividualLimits) * 100
            : 0;

        return new PoolSimulationResult(
            request.DatabaseNames,
            p95, p99, peak, mean,
            diversificationRatio,
            overloadFraction,
            recommendedDtu,
            sumIndividualLimits,
            savingsPercent);
    }

    public virtual async Task<PoolOptimizationResult> BuildPoolsAsync(
        int registeredServerId,
        BuildPoolsRequest request,
        CancellationToken cancellationToken)
    {
        var timeSeries = await metricsCacheRepository.MetricsCacheGetTimeSeriesAsync(
            registeredServerId, cancellationToken);

        var absoluteTimeSeries = ConvertToAbsoluteDtuTimeSeries(timeSeries, request.DatabaseNames, request.DtuLimits);
        var profiles = poolabilityService.BuildProfiles(absoluteTimeSeries);

        var options = new PoolOptimizerOptions(
            TargetPercentile: request.TargetPercentile,
            SafetyFactor: request.SafetyFactor,
            MaxDatabasesPerPool: request.MaxDatabasesPerPool,
            MinDiversificationRatio: request.MinDiversificationRatio);

        var initial = poolBuilder.BuildPools(profiles, options);
        return localSearchOptimizer.Improve(initial, profiles, options);
    }

    private static DtuTimeSeries ConvertToAbsoluteDtuTimeSeries(
        DtuTimeSeries timeSeries,
        IReadOnlyList<string> databaseNames,
        IReadOnlyDictionary<string, int> dtuLimits)
    {
        var absoluteValues = new Dictionary<string, IReadOnlyList<double>>();

        foreach (var name in databaseNames)
        {
            if (!timeSeries.DatabaseValues.TryGetValue(name, out var percentValues)) continue;

            var dtuLimit = dtuLimits.TryGetValue(name, out var limit) ? limit : 0;

            absoluteValues[name] = dtuLimit > 0
                ? percentValues.Select(pct => pct / 100.0 * dtuLimit).ToList()
                : percentValues;
        }

        return new DtuTimeSeries(timeSeries.Timestamps, absoluteValues);
    }

    private IReadOnlyList<IReadOnlyList<double>> ConvertToAbsoluteDtu(
        DtuTimeSeries timeSeries,
        PoolSimulationRequest request)
    {
        return request.DatabaseNames
            .Where(name => timeSeries.DatabaseValues.ContainsKey(name))
            .Select(name =>
            {
                var percentValues = timeSeries.DatabaseValues[name];
                var dtuLimit = request.DtuLimits.TryGetValue(name, out var limit) ? limit : 0;

                if (dtuLimit <= 0) return percentValues;

                return (IReadOnlyList<double>)percentValues
                    .Select(pct => pct / 100.0 * dtuLimit)
                    .ToList();
            })
            .ToList();
    }
}
