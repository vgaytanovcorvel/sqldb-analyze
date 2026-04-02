using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Implementation.Services;

public class ServerAnalysisService(
    IAzureMetricsService azureMetricsService,
    IDtuAnalysisService dtuAnalysisService) : IServerAnalysisService
{
    public virtual async Task<ElasticPoolRecommendation> AnalyzeServerAsync(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        TimeSpan timeRange,
        AnalysisTimeWindow? timeWindow,
        CancellationToken cancellationToken)
    {
        var databaseNames = await azureMetricsService.GetDatabaseNamesAsync(
            subscriptionId, resourceGroupName, serverName, cancellationToken);

        var summaries = new List<DatabaseDtuSummary>();
        foreach (var dbName in databaseNames)
        {
            var metrics = await azureMetricsService.GetDtuMetricsAsync(
                subscriptionId, resourceGroupName, serverName, dbName,
                timeRange, cancellationToken);

            if (timeWindow is not null)
            {
                metrics = dtuAnalysisService.FilterByTimeWindow(metrics, timeWindow);
            }

            var dtuLimit = await azureMetricsService.GetDatabaseDtuLimitAsync(
                subscriptionId, resourceGroupName, serverName, dbName,
                cancellationToken);

            var summary = dtuAnalysisService.Summarize(dbName, metrics, dtuLimit);
            summaries.Add(summary);
        }

        return dtuAnalysisService.Recommend(summaries);
    }
}
