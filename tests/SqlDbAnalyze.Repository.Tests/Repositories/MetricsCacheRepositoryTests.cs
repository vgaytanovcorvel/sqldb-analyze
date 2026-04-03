using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Repository.Contexts;
using SqlDbAnalyze.Repository.Entities;
using SqlDbAnalyze.Repository.Repositories;
using Xunit;

namespace SqlDbAnalyze.Repository.Tests.Repositories;

public class MetricsCacheRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly int _serverId;

    public MetricsCacheRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();

        // Seed a registered server
        context.RegisteredServers.Add(new RegisteredServerEntity
        {
            Name = "Test",
            SubscriptionId = "sub",
            ResourceGroupName = "rg",
            ServerName = "sql",
            CreatedAt = DateTimeOffset.UtcNow
        });
        context.SaveChanges();
        _serverId = context.RegisteredServers.First().RegisteredServerId;

        _factory = new TestDbContextFactory(_options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task MetricsCacheGetIntervalsAsync_Should_ReturnEmpty_When_NoMetricsExist()
    {
        // Arrange
        var repo = new MetricsCacheRepository(_factory);

        // Act
        var result = await repo.MetricsCacheGetIntervalsAsync(_serverId, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task MetricsCacheUpsertAsync_Should_InsertMetrics_When_NewDataProvided()
    {
        // Arrange
        var repo = new MetricsCacheRepository(_factory);
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2025, 1, 1, 0, 5, 0, TimeSpan.Zero);
        var timeSeries = new DtuTimeSeries(
            [t1, t2],
            new Dictionary<string, IReadOnlyList<double>>
            {
                ["DbA"] = [10.0, 20.0],
                ["DbB"] = [30.0, 40.0]
            });

        // Act
        await repo.MetricsCacheUpsertAsync(_serverId, timeSeries, CancellationToken.None);

        // Assert
        var intervals = await repo.MetricsCacheGetIntervalsAsync(_serverId, CancellationToken.None);
        intervals.Should().HaveCount(2);
        intervals.Should().Contain(i => i.DatabaseName == "DbA" && i.MetricCount == 2);
        intervals.Should().Contain(i => i.DatabaseName == "DbB" && i.MetricCount == 2);
    }

    [Fact]
    public async Task MetricsCacheUpsertAsync_Should_SkipDuplicates_When_SameTimestampsProvided()
    {
        // Arrange
        var repo = new MetricsCacheRepository(_factory);
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timeSeries = new DtuTimeSeries(
            [t1],
            new Dictionary<string, IReadOnlyList<double>> { ["DbA"] = [10.0] });

        await repo.MetricsCacheUpsertAsync(_serverId, timeSeries, CancellationToken.None);

        // Act - upsert same data again
        await repo.MetricsCacheUpsertAsync(_serverId, timeSeries, CancellationToken.None);

        // Assert
        var intervals = await repo.MetricsCacheGetIntervalsAsync(_serverId, CancellationToken.None);
        intervals.Should().HaveCount(1);
        intervals[0].MetricCount.Should().Be(1);
    }

    [Fact]
    public async Task MetricsCacheGetTimeSeriesAsync_Should_ReturnEmptyTimeSeries_When_NoMetricsExist()
    {
        // Arrange
        var repo = new MetricsCacheRepository(_factory);

        // Act
        var result = await repo.MetricsCacheGetTimeSeriesAsync(_serverId, CancellationToken.None);

        // Assert
        result.Timestamps.Should().BeEmpty();
        result.DatabaseValues.Should().BeEmpty();
    }

    [Fact]
    public async Task MetricsCacheGetTimeSeriesAsync_Should_ReturnAlignedTimeSeries_When_MetricsExist()
    {
        // Arrange
        var repo = new MetricsCacheRepository(_factory);
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2025, 1, 1, 0, 5, 0, TimeSpan.Zero);
        var timeSeries = new DtuTimeSeries(
            [t1, t2],
            new Dictionary<string, IReadOnlyList<double>>
            {
                ["DbA"] = [10.0, 20.0],
                ["DbB"] = [30.0, 40.0]
            });
        await repo.MetricsCacheUpsertAsync(_serverId, timeSeries, CancellationToken.None);

        // Act
        var result = await repo.MetricsCacheGetTimeSeriesAsync(_serverId, CancellationToken.None);

        // Assert
        result.Timestamps.Should().HaveCount(2);
        result.DatabaseValues.Should().HaveCount(2);
        result.DatabaseValues["DbA"].Should().BeEquivalentTo([10.0, 20.0]);
        result.DatabaseValues["DbB"].Should().BeEquivalentTo([30.0, 40.0]);
    }

    [Fact]
    public async Task MetricsCacheDeleteByServerAsync_Should_RemoveAllMetrics_When_MetricsExist()
    {
        // Arrange
        var repo = new MetricsCacheRepository(_factory);
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timeSeries = new DtuTimeSeries(
            [t1],
            new Dictionary<string, IReadOnlyList<double>> { ["DbA"] = [10.0] });
        await repo.MetricsCacheUpsertAsync(_serverId, timeSeries, CancellationToken.None);

        // Act
        await repo.MetricsCacheDeleteByServerAsync(_serverId, CancellationToken.None);

        // Assert
        var intervals = await repo.MetricsCacheGetIntervalsAsync(_serverId, CancellationToken.None);
        intervals.Should().BeEmpty();
    }

    [Fact]
    public async Task MetricsCacheGetIntervalsAsync_Should_ReturnCorrectTimestamps_When_MetricsExist()
    {
        // Arrange
        var repo = new MetricsCacheRepository(_factory);
        var earliest = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var latest = new DateTimeOffset(2025, 1, 1, 1, 0, 0, TimeSpan.Zero);
        var timeSeries = new DtuTimeSeries(
            [earliest, latest],
            new Dictionary<string, IReadOnlyList<double>> { ["DbA"] = [10.0, 90.0] });
        await repo.MetricsCacheUpsertAsync(_serverId, timeSeries, CancellationToken.None);

        // Act
        var intervals = await repo.MetricsCacheGetIntervalsAsync(_serverId, CancellationToken.None);

        // Assert
        intervals.Should().HaveCount(1);
        intervals[0].EarliestTimestamp.Should().Be(earliest);
        intervals[0].LatestTimestamp.Should().Be(latest);
        intervals[0].MetricCount.Should().Be(2);
    }

    private class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
