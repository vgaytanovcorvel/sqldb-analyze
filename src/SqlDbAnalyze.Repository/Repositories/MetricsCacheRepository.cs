using Microsoft.EntityFrameworkCore;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Repository.Contexts;
using SqlDbAnalyze.Repository.Entities;

namespace SqlDbAnalyze.Repository.Repositories;

public class MetricsCacheRepository(
    IDbContextFactory<AppDbContext> contextFactory)
    : RepositoryBase<AppDbContext>(contextFactory), IMetricsCacheRepository
{
    public virtual async Task<IReadOnlyList<DatabaseMetricsInterval>> MetricsCacheGetIntervalsAsync(
        int registeredServerId, CancellationToken cancellationToken)
    {
        await using var dbContext = await CreateContextAsync(cancellationToken);

        var metrics = await dbContext.CachedDtuMetrics
            .AsNoTracking()
            .Where(e => e.RegisteredServerId == registeredServerId)
            .Select(e => new { e.DatabaseName, e.Timestamp })
            .ToListAsync(cancellationToken);

        return metrics
            .GroupBy(e => e.DatabaseName)
            .Select(g => new DatabaseMetricsInterval(
                g.Key,
                g.Min(e => e.Timestamp),
                g.Max(e => e.Timestamp),
                g.Count()))
            .OrderBy(i => i.DatabaseName)
            .ToList();
    }

    public virtual async Task<DtuTimeSeries> MetricsCacheGetTimeSeriesAsync(
        int registeredServerId, CancellationToken cancellationToken)
    {
        await using var dbContext = await CreateContextAsync(cancellationToken);

        var metrics = await dbContext.CachedDtuMetrics
            .AsNoTracking()
            .Where(e => e.RegisteredServerId == registeredServerId)
            .ToListAsync(cancellationToken);

        var sorted = metrics.OrderBy(e => e.Timestamp).ToList();
        return BuildTimeSeries(sorted);
    }

    public virtual async Task MetricsCacheUpsertAsync(
        int registeredServerId, DtuTimeSeries timeSeries, CancellationToken cancellationToken)
    {
        await using var dbContext = await CreateContextAsync(cancellationToken);

        var entities = new List<CachedDtuMetricEntity>();

        for (var i = 0; i < timeSeries.Timestamps.Count; i++)
        {
            var timestamp = timeSeries.Timestamps[i];
            foreach (var (dbName, values) in timeSeries.DatabaseValues)
            {
                entities.Add(new CachedDtuMetricEntity
                {
                    RegisteredServerId = registeredServerId,
                    DatabaseName = dbName,
                    Timestamp = timestamp,
                    DtuPercentage = values[i]
                });
            }
        }

        var existingTimestamps = await dbContext.CachedDtuMetrics
            .Where(e => e.RegisteredServerId == registeredServerId)
            .Select(e => new { e.DatabaseName, e.Timestamp })
            .ToListAsync(cancellationToken);

        var existingSet = existingTimestamps
            .Select(e => (e.DatabaseName, e.Timestamp))
            .ToHashSet();

        var newEntities = entities
            .Where(e => !existingSet.Contains((e.DatabaseName, e.Timestamp)))
            .ToList();

        if (newEntities.Count > 0)
        {
            await dbContext.CachedDtuMetrics.AddRangeAsync(newEntities, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public virtual async Task MetricsCacheDeleteByServerAsync(
        int registeredServerId, CancellationToken cancellationToken)
    {
        await using var dbContext = await CreateContextAsync(cancellationToken);

        await dbContext.CachedDtuMetrics
            .Where(e => e.RegisteredServerId == registeredServerId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static DtuTimeSeries BuildTimeSeries(List<CachedDtuMetricEntity> metrics)
    {
        if (metrics.Count == 0)
        {
            return new DtuTimeSeries(
                Array.Empty<DateTimeOffset>(),
                new Dictionary<string, IReadOnlyList<double>>());
        }

        var timestamps = metrics
            .Select(m => m.Timestamp)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        var timestampIndex = timestamps
            .Select((t, i) => (t, i))
            .ToDictionary(x => x.t, x => x.i);

        var dbNames = metrics
            .Select(m => m.DatabaseName)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var databaseValues = new Dictionary<string, IReadOnlyList<double>>();

        foreach (var dbName in dbNames)
        {
            var values = new double[timestamps.Count];
            foreach (var metric in metrics.Where(m => m.DatabaseName == dbName))
            {
                if (timestampIndex.TryGetValue(metric.Timestamp, out var idx))
                {
                    values[idx] = metric.DtuPercentage;
                }
            }
            databaseValues[dbName] = values;
        }

        return new DtuTimeSeries(timestamps, databaseValues);
    }
}
