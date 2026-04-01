using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface IServerAnalysisService
{
    Task<ElasticPoolRecommendation> AnalyzeServerAsync(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        TimeSpan timeRange,
        CancellationToken cancellationToken);
}
