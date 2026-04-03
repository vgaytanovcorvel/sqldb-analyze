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

    public virtual async Task<IReadOnlyList<DatabaseInfo>> GetDatabaseInfoAsync(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        CancellationToken cancellationToken)
    {
        var serverResourceId = SqlServerResource.CreateResourceIdentifier(
            subscriptionId, resourceGroupName, serverName);

        var server = armClient.GetSqlServerResource(serverResourceId);

        var databases = new List<DatabaseInfo>();
        await foreach (var db in server.GetSqlDatabases()
            .GetAllAsync(cancellationToken: cancellationToken))
        {
            if (string.Equals(db.Data.Name, "master", StringComparison.OrdinalIgnoreCase))
                continue;

            var dataSizeMb = (db.Data.MaxSizeBytes ?? 0) / (1024.0 * 1024.0);
            var elasticPoolName = ExtractElasticPoolName(db.Data.ElasticPoolId);
            var dtuLimit = ResolveDtuLimit(db.Data.CurrentServiceObjectiveName, elasticPoolName);

            databases.Add(new DatabaseInfo(db.Data.Name, dataSizeMb, dtuLimit, elasticPoolName));
        }

        return databases;
    }

    private static string? ExtractElasticPoolName(Azure.Core.ResourceIdentifier? elasticPoolId)
    {
        if (elasticPoolId is null) return null;

        var segments = elasticPoolId.ToString().Split('/');
        return segments.Length > 0 ? segments[^1] : null;
    }

    private static int ResolveDtuLimit(string? serviceObjectiveName, string? elasticPoolName)
    {
        if (string.IsNullOrEmpty(serviceObjectiveName)) return 0;

        if (elasticPoolName is not null)
            return MapElasticPoolDtuLimit(serviceObjectiveName);

        return MapStandaloneDtuLimit(serviceObjectiveName);
    }

    private static int MapStandaloneDtuLimit(string serviceObjectiveName) =>
        serviceObjectiveName switch
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
            _ => 0
        };

    private static int MapElasticPoolDtuLimit(string serviceObjectiveName) =>
        serviceObjectiveName switch
        {
            "ElasticPool" => 0,
            _ when serviceObjectiveName.StartsWith("GP_") => 0,
            _ => MapStandaloneDtuLimit(serviceObjectiveName)
        };

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

        var limit = MapStandaloneDtuLimit(response.Value.Data.CurrentServiceObjectiveName ?? "");

        if (limit == 0 && response.Value.Data.CurrentServiceObjectiveName?.StartsWith("GP_") != true)
        {
            throw new AzureResourceNotFoundException(
                $"Unknown service objective: {response.Value.Data.CurrentServiceObjectiveName}");
        }

        return limit;
    }
}
