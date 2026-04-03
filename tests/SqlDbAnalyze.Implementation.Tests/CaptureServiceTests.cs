using FluentAssertions;
using NSubstitute;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class CaptureServiceTests
{
    private readonly IAzureMetricsService _azureMetrics = Substitute.For<IAzureMetricsService>();
    private readonly IDtuAnalysisService _dtuAnalysis = Substitute.For<IDtuAnalysisService>();
    private readonly CaptureService _sut;

    private const string Sub = "sub-1";
    private const string Rg = "rg-1";
    private const string Server = "sql-1";

    public CaptureServiceTests()
    {
        _sut = new CaptureService(_azureMetrics, _dtuAnalysis);
    }

    [Fact]
    public async Task CaptureMetricsAsync_ShouldReturnEmptyTimeSeries_WhenNoDatabases()
    {
        // Arrange
        _azureMetrics.GetDatabaseNamesAsync(Sub, Rg, Server, Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        // Act
        var result = await _sut.CaptureMetricsAsync(Sub, Rg, Server,
            TimeSpan.FromHours(24), null, CancellationToken.None);

        // Assert
        result.Timestamps.Should().BeEmpty();
        result.DatabaseValues.Should().BeEmpty();
    }

    [Fact]
    public async Task CaptureMetricsAsync_ShouldAlignTimestamps_WhenMultipleDatabases()
    {
        // Arrange
        var t1 = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 1, 1, 0, 5, 0, TimeSpan.Zero);

        _azureMetrics.GetDatabaseNamesAsync(Sub, Rg, Server, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "db1", "db2" });

        _azureMetrics.GetDtuMetricsAsync(Sub, Rg, Server, "db1", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new List<DtuMetric>
            {
                new("db1", t1, 10.0),
                new("db1", t2, 20.0)
            });

        _azureMetrics.GetDtuMetricsAsync(Sub, Rg, Server, "db2", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new List<DtuMetric>
            {
                new("db2", t1, 30.0),
                new("db2", t2, 40.0)
            });

        // Act
        var result = await _sut.CaptureMetricsAsync(Sub, Rg, Server,
            TimeSpan.FromHours(24), null, CancellationToken.None);

        // Assert
        result.Timestamps.Should().HaveCount(2);
        result.DatabaseValues.Should().HaveCount(2);
        result.DatabaseValues["db1"].Should().BeEquivalentTo([10.0, 20.0]);
        result.DatabaseValues["db2"].Should().BeEquivalentTo([30.0, 40.0]);
    }

    [Fact]
    public async Task CaptureMetricsAsync_ShouldFilterByTimeWindow_WhenWindowProvided()
    {
        // Arrange
        var t1 = new DateTimeOffset(2026, 1, 1, 14, 0, 0, TimeSpan.Zero); // within 09-17 ET
        var allMetrics = new List<DtuMetric> { new("db1", t1, 50.0) };
        var filteredMetrics = new List<DtuMetric> { new("db1", t1, 50.0) };
        var timeWindow = new AnalysisTimeWindow(new TimeOnly(9, 0), new TimeOnly(17, 0), "Eastern Standard Time");

        _azureMetrics.GetDatabaseNamesAsync(Sub, Rg, Server, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "db1" });

        _azureMetrics.GetDtuMetricsAsync(Sub, Rg, Server, "db1", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(allMetrics);

        _dtuAnalysis.FilterByTimeWindow(allMetrics, timeWindow)
            .Returns(filteredMetrics);

        // Act
        var result = await _sut.CaptureMetricsAsync(Sub, Rg, Server,
            TimeSpan.FromHours(24), timeWindow, CancellationToken.None);

        // Assert
        _dtuAnalysis.Received(1).FilterByTimeWindow(allMetrics, timeWindow);
        result.Timestamps.Should().HaveCount(1);
    }

    [Fact]
    public async Task CaptureMetricsAsync_ShouldFillZeros_WhenDatabaseMissesTimestamps()
    {
        // Arrange
        var t1 = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 1, 1, 0, 5, 0, TimeSpan.Zero);

        _azureMetrics.GetDatabaseNamesAsync(Sub, Rg, Server, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "db1", "db2" });

        // db1 has both timestamps, db2 only has t1
        _azureMetrics.GetDtuMetricsAsync(Sub, Rg, Server, "db1", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new List<DtuMetric>
            {
                new("db1", t1, 10.0),
                new("db1", t2, 20.0)
            });

        _azureMetrics.GetDtuMetricsAsync(Sub, Rg, Server, "db2", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new List<DtuMetric>
            {
                new("db2", t1, 30.0)
            });

        // Act
        var result = await _sut.CaptureMetricsAsync(Sub, Rg, Server,
            TimeSpan.FromHours(24), null, CancellationToken.None);

        // Assert
        result.Timestamps.Should().HaveCount(2);
        result.DatabaseValues["db2"][0].Should().Be(30.0);
        result.DatabaseValues["db2"][1].Should().Be(0.0); // filled with zero
    }

    [Fact]
    public async Task CaptureMetricsAsync_ShouldSnapTimestampsToFiveMinutes_WhenUnaligned()
    {
        // Arrange -- timestamps not exactly on 5-minute boundaries
        var t1 = new DateTimeOffset(2026, 1, 1, 0, 2, 30, TimeSpan.Zero); // should snap to 0:00
        var t2 = new DateTimeOffset(2026, 1, 1, 0, 7, 15, TimeSpan.Zero); // should snap to 0:05

        _azureMetrics.GetDatabaseNamesAsync(Sub, Rg, Server, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "db1" });

        _azureMetrics.GetDtuMetricsAsync(Sub, Rg, Server, "db1", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new List<DtuMetric>
            {
                new("db1", t1, 10.0),
                new("db1", t2, 20.0)
            });

        // Act
        var result = await _sut.CaptureMetricsAsync(Sub, Rg, Server,
            TimeSpan.FromHours(24), null, CancellationToken.None);

        // Assert
        result.Timestamps.Should().HaveCount(2);
        result.Timestamps[0].Minute.Should().Be(0);
        result.Timestamps[1].Minute.Should().Be(5);
    }
}
