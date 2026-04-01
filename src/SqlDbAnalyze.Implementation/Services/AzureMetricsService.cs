using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure.ResourceManager;
using Azure.ResourceManager.Sql;
using SqlDbAnalyze.Abstractions.Exceptions;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Implementation.Services;

public class AzureMetricsService(
    ArmClient armClient,
    MetricsQueryClient metricsClient) : IAzureMetricsService
{
    public virtual async Task<IReadOnlyList<string>> GetDatabaseNamesAsync(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        CancellationToken cancellationToken)
    {
        var serverResourceId = SqlServerResource.CreateResourceIdentifier(
            subscriptionId, resourceGroupName, serverName);

        var server = armClient.GetSqlServerResource(serverResourceId);

        var databases = new List<string>();
        await foreach (var db in server.GetSqlDatabases()
            .GetAllAsync(cancellationToken: cancellationToken))
        {
            if (!string.Equals(db.Data.Name, "master", StringComparison.OrdinalIgnoreCase))
            {
                databases.Add(db.Data.Name);
            }
        }

        return databases;
    }

    public virtual async Task<IReadOnlyList<DtuMetric>> GetDtuMetricsAsync(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        string databaseName,
        TimeSpan timeRange,
        CancellationToken cancellationToken)
    {
        var resourceId = SqlDatabaseResource.CreateResourceIdentifier(
            subscriptionId, resourceGroupName, serverName, databaseName);

        var response = await metricsClient.QueryResourceAsync(
            resourceId.ToString(),
            new[] { "dtu_consumption_percent" },
            new MetricsQueryOptions
            {
                TimeRange = new QueryTimeRange(timeRange),
                Granularity = TimeSpan.FromMinutes(5)
            },
            cancellationToken);

        var metrics = new List<DtuMetric>();
        foreach (var metric in response.Value.Metrics)
        {
            foreach (var timeSeries in metric.TimeSeries)
            {
                foreach (var value in timeSeries.Values)
                {
                    if (value.Average.HasValue)
                    {
                        metrics.Add(new DtuMetric(
                            databaseName,
                            value.TimeStamp,
                            value.Average.Value));
                    }
                }
            }
        }

        return metrics;
    }

    public virtual async Task<int> GetDatabaseDtuLimitAsync(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        string databaseName,
        CancellationToken cancellationToken)
    {
        var databaseResourceId = SqlDatabaseResource.CreateResourceIdentifier(
            subscriptionId, resourceGroupName, serverName, databaseName);

        var database = armClient.GetSqlDatabaseResource(databaseResourceId);
        var response = await database.GetAsync(cancellationToken);

        return (int)(response.Value.Data.CurrentServiceObjectiveName switch
        {
            "Basic" => 5,
            "S0" => 10,
            "S1" => 20,
            "S2" => 50,
            "S3" => 100,
            "S4" => 200,
            "S6" => 400,
            "S7" => 800,
            "S9" => 1600,
            "S12" => 3000,
            "P1" => 125,
            "P2" => 250,
            "P4" => 500,
            "P6" => 1000,
            "P11" => 1750,
            "P15" => 4000,
            _ => response.Value.Data.CurrentServiceObjectiveName?.StartsWith("GP_") == true
                ? 0
                : throw new AzureResourceNotFoundException(
                    $"Unknown service objective: {response.Value.Data.CurrentServiceObjectiveName}")
        });
    }
}
