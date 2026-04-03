using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface IAzureMetricsService
{
    Task<IReadOnlyList<string>> GetDatabaseNamesAsync(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DatabaseInfo>> GetDatabaseInfoAsync(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DtuMetric>> GetDtuMetricsAsync(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        string databaseName,
        TimeSpan timeRange,
        CancellationToken cancellationToken);

    Task<int> GetDatabaseDtuLimitAsync(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        string databaseName,
        CancellationToken cancellationToken);
}
