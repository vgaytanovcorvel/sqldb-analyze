using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Implementation.Services;

public class CaptureService(
    IAzureMetricsService azureMetricsService,
    IDtuAnalysisService dtuAnalysisService) : ICaptureService
{
    public virtual async Task<DtuTimeSeries> CaptureMetricsAsync(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        TimeSpan timeRange,
        AnalysisTimeWindow? timeWindow,
        CancellationToken cancellationToken)
    {
        var databaseNames = await azureMetricsService.GetDatabaseNamesAsync(
            subscriptionId, resourceGroupName, serverName, cancellationToken);

        var allMetrics = new Dictionary<string, IReadOnlyList<DtuMetric>>();
        foreach (var dbName in databaseNames)
        {
            var metrics = await FetchFilteredMetrics(
                subscriptionId, resourceGroupName, serverName, dbName,
                timeRange, timeWindow, cancellationToken);
            allMetrics[dbName] = metrics;
        }

        return AlignMetrics(allMetrics);
    }

    private async Task<IReadOnlyList<DtuMetric>> FetchFilteredMetrics(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        string dbName,
        TimeSpan timeRange,
        AnalysisTimeWindow? timeWindow,
        CancellationToken cancellationToken)
    {
        var metrics = await azureMetricsService.GetDtuMetricsAsync(
            subscriptionId, resourceGroupName, serverName, dbName,
            timeRange, cancellationToken);

        return timeWindow is not null
            ? dtuAnalysisService.FilterByTimeWindow(metrics, timeWindow)
            : metrics;
    }

    private static DtuTimeSeries AlignMetrics(Dictionary<string, IReadOnlyList<DtuMetric>> allMetrics)
    {
        var allTimestamps = allMetrics.Values
            .SelectMany(m => m.Select(x => SnapToFiveMinutes(x.Timestamp)))
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        var timestampIndex = allTimestamps
            .Select((t, i) => (t, i))
            .ToDictionary(x => x.t, x => x.i);

        var databaseValues = new Dictionary<string, IReadOnlyList<double>>();
        foreach (var (dbName, metrics) in allMetrics)
            databaseValues[dbName] = BuildAlignedValues(metrics, allTimestamps, timestampIndex);

        return new DtuTimeSeries(allTimestamps, databaseValues);
    }

    private static IReadOnlyList<double> BuildAlignedValues(
        IReadOnlyList<DtuMetric> metrics,
        List<DateTimeOffset> allTimestamps,
        Dictionary<DateTimeOffset, int> timestampIndex)
    {
        var values = new double[allTimestamps.Count];

        foreach (var metric in metrics)
        {
            var snapped = SnapToFiveMinutes(metric.Timestamp);
            if (timestampIndex.TryGetValue(snapped, out var idx))
                values[idx] = metric.DtuPercentage;
        }

        return values;
    }

    private static DateTimeOffset SnapToFiveMinutes(DateTimeOffset timestamp)
    {
        var ticks = timestamp.Ticks;
        var fiveMinTicks = TimeSpan.FromMinutes(5).Ticks;
        var snapped = ticks / fiveMinTicks * fiveMinTicks;
        return new DateTimeOffset(snapped, timestamp.Offset);
    }
}
