using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface ICaptureService
{
    Task<DtuTimeSeries> CaptureMetricsAsync(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        TimeSpan timeRange,
        AnalysisTimeWindow? timeWindow,
        CancellationToken cancellationToken);
}
