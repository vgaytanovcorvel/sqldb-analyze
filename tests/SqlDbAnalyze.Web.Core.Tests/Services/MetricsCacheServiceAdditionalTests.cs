using FluentAssertions;
using NSubstitute;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Web.Core.Services;
using Xunit;

namespace SqlDbAnalyze.Web.Core.Tests.Services;

public class MetricsCacheServiceAdditionalTests
{
    private readonly IRegisteredServerRepository _serverRepo = Substitute.For<IRegisteredServerRepository>();
    private readonly IMetricsCacheRepository _cacheRepo = Substitute.For<IMetricsCacheRepository>();
    private readonly IAzureMetricsService _azureMetrics = Substitute.For<IAzureMetricsService>();
    private readonly ICaptureService _captureService = Substitute.For<ICaptureService>();
    private readonly IPoolabilityService _poolabilityService = Substitute.For<IPoolabilityService>();
    private readonly IStatisticsService _statisticsService = Substitute.For<IStatisticsService>();
    private readonly IPoolBuilder _poolBuilder = Substitute.For<IPoolBuilder>();
    private readonly ILocalSearchOptimizer _localSearchOptimizer = Substitute.For<ILocalSearchOptimizer>();

    private static readonly RegisteredServer TestServer =
        new(1, "Test", "sub-1", "rg-1", "sql-1", DateTimeOffset.UtcNow);

    private MetricsCacheService CreateService() =>
        new(_serverRepo, _cacheRepo, _azureMetrics, _captureService,
            _poolabilityService, _statisticsService, _poolBuilder, _localSearchOptimizer);

    [Fact]
    public async Task GetDatabaseInfoAsync_Should_ReturnDatabaseInfo_When_ServerExists()
    {
        // Arrange
        _serverRepo.RegisteredServerSingleByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestServer);
        var databases = new List<DatabaseInfo>
        {
            new("DbA", 1024.0, 100, null),
            new("DbB", 2048.0, 200, "pool-1")
        };
        _azureMetrics.GetDatabaseInfoAsync("sub-1", "rg-1", "sql-1", Arg.Any<CancellationToken>())
            .Returns(databases);
        var service = CreateService();

        // Act
        var result = await service.GetDatabaseInfoAsync(1, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].DatabaseName.Should().Be("DbA");
        result[1].ElasticPoolName.Should().Be("pool-1");
    }

    [Fact]
    public async Task SimulatePoolAsync_Should_ReturnSimulationResult_When_DataExists()
    {
        // Arrange
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timeSeries = new DtuTimeSeries(
            [t1],
            new Dictionary<string, IReadOnlyList<double>>
            {
                ["db1"] = [50.0],
                ["db2"] = [80.0]
            });
        _cacheRepo.MetricsCacheGetTimeSeriesAsync(1, Arg.Any<CancellationToken>()).Returns(timeSeries);

        var request = new PoolSimulationRequest(
            new List<string> { "db1", "db2" },
            new Dictionary<string, int> { ["db1"] = 100, ["db2"] = 200 });

        // Convert to absolute: db1 = 50/100*100=50, db2 = 80/100*200=160
        // Combined = [210]
        _statisticsService.SumSeries(Arg.Any<IReadOnlyList<IReadOnlyList<double>>>())
            .Returns(new double[] { 210.0 });
        _statisticsService.Percentile(Arg.Any<IReadOnlyList<double>>(), 0.95).Returns(210.0);
        _statisticsService.Percentile(Arg.Any<IReadOnlyList<double>>(), 0.99).Returns(210.0);
        _statisticsService.Mean(Arg.Any<IReadOnlyList<double>>()).Returns(210.0);
        _statisticsService.OverloadFraction(Arg.Any<IReadOnlyList<double>>(), Arg.Any<double>()).Returns(0.0);

        var service = CreateService();

        // Act
        var result = await service.SimulatePoolAsync(1, request, CancellationToken.None);

        // Assert
        result.DatabaseNames.Should().HaveCount(2);
        result.SumIndividualDtuLimits.Should().Be(300);
        result.P95Dtu.Should().Be(210.0);
        result.P99Dtu.Should().Be(210.0);
    }

    [Fact]
    public async Task BuildPoolsAsync_Should_ReturnOptimizationResult_When_DataExists()
    {
        // Arrange
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timeSeries = new DtuTimeSeries(
            [t1],
            new Dictionary<string, IReadOnlyList<double>>
            {
                ["db1"] = [50.0],
                ["db2"] = [80.0]
            });
        _cacheRepo.MetricsCacheGetTimeSeriesAsync(1, Arg.Any<CancellationToken>()).Returns(timeSeries);

        var profiles = new List<DatabaseProfile>
        {
            new("db1", [50.0], 50.0, 50.0, 50.0, 50.0),
            new("db2", [160.0], 160.0, 160.0, 160.0, 160.0)
        };
        _poolabilityService.BuildProfiles(Arg.Any<DtuTimeSeries>()).Returns(profiles);

        var initialResult = new PoolOptimizationResult(
            new List<PoolAssignment>
            {
                new(0, new List<string> { "db1", "db2" }, 200.0, 180.0, 195.0, 210.0, 1.5, 0.0)
            },
            200.0, new List<string>());
        _poolBuilder.BuildPools(profiles, Arg.Any<PoolOptimizerOptions>()).Returns(initialResult);
        _localSearchOptimizer.Improve(initialResult, profiles, Arg.Any<PoolOptimizerOptions>()).Returns(initialResult);

        var request = new BuildPoolsRequest(
            new List<string> { "db1", "db2" },
            new Dictionary<string, int> { ["db1"] = 100, ["db2"] = 200 });
        var service = CreateService();

        // Act
        var result = await service.BuildPoolsAsync(1, request, CancellationToken.None);

        // Assert
        result.Pools.Should().HaveCount(1);
        result.TotalRequiredCapacity.Should().Be(200.0);
    }

    [Fact]
    public async Task SimulatePoolAsync_Should_HandleMissingDtuLimits_WhenDatabaseNotInLimitsMap()
    {
        // Arrange
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timeSeries = new DtuTimeSeries(
            [t1],
            new Dictionary<string, IReadOnlyList<double>>
            {
                ["db1"] = [50.0]
            });
        _cacheRepo.MetricsCacheGetTimeSeriesAsync(1, Arg.Any<CancellationToken>()).Returns(timeSeries);

        var request = new PoolSimulationRequest(
            new List<string> { "db1" },
            new Dictionary<string, int>()); // no DTU limits

        _statisticsService.SumSeries(Arg.Any<IReadOnlyList<IReadOnlyList<double>>>())
            .Returns(new double[] { 50.0 });
        _statisticsService.Percentile(Arg.Any<IReadOnlyList<double>>(), 0.95).Returns(50.0);
        _statisticsService.Percentile(Arg.Any<IReadOnlyList<double>>(), 0.99).Returns(50.0);
        _statisticsService.Mean(Arg.Any<IReadOnlyList<double>>()).Returns(50.0);
        _statisticsService.OverloadFraction(Arg.Any<IReadOnlyList<double>>(), Arg.Any<double>()).Returns(0.0);

        var service = CreateService();

        // Act
        var result = await service.SimulatePoolAsync(1, request, CancellationToken.None);

        // Assert
        result.SumIndividualDtuLimits.Should().Be(0);
        result.EstimatedSavingsPercent.Should().Be(0);
    }

    [Fact]
    public async Task SimulatePoolAsync_Should_HandleMissingDatabase_WhenDatabaseNotInTimeSeries()
    {
        // Arrange
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timeSeries = new DtuTimeSeries(
            [t1],
            new Dictionary<string, IReadOnlyList<double>>
            {
                ["db1"] = [50.0]
            });
        _cacheRepo.MetricsCacheGetTimeSeriesAsync(1, Arg.Any<CancellationToken>()).Returns(timeSeries);

        // Request includes db2 which is not in the time series
        var request = new PoolSimulationRequest(
            new List<string> { "db1", "db2" },
            new Dictionary<string, int> { ["db1"] = 100, ["db2"] = 200 });

        _statisticsService.SumSeries(Arg.Any<IReadOnlyList<IReadOnlyList<double>>>())
            .Returns(new double[] { 50.0 });
        _statisticsService.Percentile(Arg.Any<IReadOnlyList<double>>(), 0.95).Returns(50.0);
        _statisticsService.Percentile(Arg.Any<IReadOnlyList<double>>(), 0.99).Returns(50.0);
        _statisticsService.Mean(Arg.Any<IReadOnlyList<double>>()).Returns(50.0);
        _statisticsService.OverloadFraction(Arg.Any<IReadOnlyList<double>>(), Arg.Any<double>()).Returns(0.0);

        var service = CreateService();

        // Act
        var result = await service.SimulatePoolAsync(1, request, CancellationToken.None);

        // Assert -- db2 is skipped for load calculation (not in time series)
        // but SumIndividualDtuLimits counts ALL databases in request that have DTU limits
        result.SumIndividualDtuLimits.Should().Be(300);
    }

    [Fact]
    public async Task GetCorrelationMatrixAsync_Should_ReturnCorrectPairCount_WhenThreeDatabases()
    {
        // Arrange
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timeSeries = new DtuTimeSeries(
            [t1],
            new Dictionary<string, IReadOnlyList<double>>
            {
                ["DbA"] = [10.0], ["DbB"] = [20.0], ["DbC"] = [30.0]
            });
        _cacheRepo.MetricsCacheGetTimeSeriesAsync(1, Arg.Any<CancellationToken>()).Returns(timeSeries);

        var profiles = new List<DatabaseProfile>
        {
            new("DbA", [10.0], 10.0, 10.0, 10.0, 10.0),
            new("DbB", [20.0], 20.0, 20.0, 20.0, 20.0),
            new("DbC", [30.0], 30.0, 30.0, 30.0, 30.0)
        };
        _poolabilityService.BuildProfiles(timeSeries).Returns(profiles);
        _poolabilityService.ComputePairwise(Arg.Any<DatabaseProfile>(), Arg.Any<DatabaseProfile>(), 0.80)
            .Returns(new PoolabilityMetrics("A", "B", 0.0, 0.0, 0.0, 0.0));

        var service = CreateService();

        // Act
        var result = await service.GetCorrelationMatrixAsync(1, CancellationToken.None);

        // Assert -- 3 databases = C(3,2) = 3 pairs
        result.Should().HaveCount(3);
    }
}
