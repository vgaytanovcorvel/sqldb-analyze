using FluentAssertions;
using NSubstitute;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Web.Core.Services;
using Xunit;

namespace SqlDbAnalyze.Web.Core.Tests.Services;

public class MetricsCacheServiceTests
{
    private readonly IRegisteredServerRepository _serverRepo = Substitute.For<IRegisteredServerRepository>();
    private readonly IMetricsCacheRepository _cacheRepo = Substitute.For<IMetricsCacheRepository>();
    private readonly IAzureMetricsService _azureMetrics = Substitute.For<IAzureMetricsService>();
    private readonly ICaptureService _captureService = Substitute.For<ICaptureService>();
    private readonly IPoolabilityService _poolabilityService = Substitute.For<IPoolabilityService>();
    private readonly IStatisticsService _statisticsService = Substitute.For<IStatisticsService>();
    private readonly IPoolBuilder _poolBuilder = Substitute.For<IPoolBuilder>();
    private readonly ILocalSearchOptimizer _localSearchOptimizer = Substitute.For<ILocalSearchOptimizer>();

    private MetricsCacheService CreateService() =>
        new(_serverRepo, _cacheRepo, _azureMetrics, _captureService, _poolabilityService, _statisticsService, _poolBuilder, _localSearchOptimizer);

    private static readonly RegisteredServer TestServer = new(1, "Test", "sub-1", "rg-1", "sql-1", DateTimeOffset.UtcNow);

    [Fact]
    public async Task GetDatabaseNamesAsync_Should_ReturnNames_When_ServerExists()
    {
        // Arrange
        _serverRepo.RegisteredServerSingleByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestServer);
        _azureMetrics.GetDatabaseNamesAsync("sub-1", "rg-1", "sql-1", Arg.Any<CancellationToken>())
            .Returns(new List<string> { "DbA", "DbB" });
        var service = CreateService();

        // Act
        var result = await service.GetDatabaseNamesAsync(1, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(["DbA", "DbB"]);
    }

    [Fact]
    public async Task GetCachedIntervalsAsync_Should_DelegateToRepository_When_Called()
    {
        // Arrange
        var intervals = new List<DatabaseMetricsInterval>
        {
            new("DbA", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow, 100)
        };
        _cacheRepo.MetricsCacheGetIntervalsAsync(1, Arg.Any<CancellationToken>()).Returns(intervals);
        var service = CreateService();

        // Act
        var result = await service.GetCachedIntervalsAsync(1, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].DatabaseName.Should().Be("DbA");
    }

    [Fact]
    public async Task RefreshMetricsAsync_Should_CaptureAndCache_When_Called()
    {
        // Arrange
        _serverRepo.RegisteredServerSingleByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestServer);
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var captured = new DtuTimeSeries([t1], new Dictionary<string, IReadOnlyList<double>> { ["DbA"] = [50.0] });
        _captureService.CaptureMetricsAsync("sub-1", "rg-1", "sql-1", TimeSpan.FromHours(168), null, Arg.Any<CancellationToken>())
            .Returns(captured);
        _cacheRepo.MetricsCacheGetTimeSeriesAsync(1, Arg.Any<CancellationToken>()).Returns(captured);
        var service = CreateService();

        // Act
        var result = await service.RefreshMetricsAsync(1, 168, CancellationToken.None);

        // Assert
        await _cacheRepo.Received(1).MetricsCacheUpsertAsync(1, captured, Arg.Any<CancellationToken>());
        result.Timestamps.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCachedTimeSeriesAsync_Should_DelegateToRepository_When_Called()
    {
        // Arrange
        var timeSeries = new DtuTimeSeries([], new Dictionary<string, IReadOnlyList<double>>());
        _cacheRepo.MetricsCacheGetTimeSeriesAsync(1, Arg.Any<CancellationToken>()).Returns(timeSeries);
        var service = CreateService();

        // Act
        var result = await service.GetCachedTimeSeriesAsync(1, CancellationToken.None);

        // Assert
        result.Timestamps.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCorrelationMatrixAsync_Should_ReturnPairwiseMetrics_When_DataExists()
    {
        // Arrange
        var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timeSeries = new DtuTimeSeries(
            [t1],
            new Dictionary<string, IReadOnlyList<double>> { ["DbA"] = [50.0], ["DbB"] = [30.0] });
        _cacheRepo.MetricsCacheGetTimeSeriesAsync(1, Arg.Any<CancellationToken>()).Returns(timeSeries);

        var profiles = new List<DatabaseProfile>
        {
            new("DbA", [50.0], 50.0, 50.0, 50.0, 50.0),
            new("DbB", [30.0], 30.0, 30.0, 30.0, 30.0)
        };
        _poolabilityService.BuildProfiles(timeSeries).Returns(profiles);
        _poolabilityService.ComputePairwise(profiles[0], profiles[1], 0.80)
            .Returns(new PoolabilityMetrics("DbA", "DbB", 0.5, 0.3, 0.1, 0.2));
        var service = CreateService();

        // Act
        var result = await service.GetCorrelationMatrixAsync(1, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].DatabaseA.Should().Be("DbA");
        result[0].DatabaseB.Should().Be("DbB");
        result[0].PearsonCorrelation.Should().Be(0.5);
    }

    [Fact]
    public async Task GetCorrelationMatrixAsync_Should_ReturnEmpty_When_NoData()
    {
        // Arrange
        var timeSeries = new DtuTimeSeries([], new Dictionary<string, IReadOnlyList<double>>());
        _cacheRepo.MetricsCacheGetTimeSeriesAsync(1, Arg.Any<CancellationToken>()).Returns(timeSeries);
        _poolabilityService.BuildProfiles(timeSeries).Returns(new List<DatabaseProfile>());
        var service = CreateService();

        // Act
        var result = await service.GetCorrelationMatrixAsync(1, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
