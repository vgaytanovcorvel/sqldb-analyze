using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class PoolabilityServiceTests
{
    private readonly StatisticsService statisticsService = new();
    private readonly PoolabilityService sut;

    public PoolabilityServiceTests()
    {
        sut = new PoolabilityService(statisticsService);
    }

    // --- BuildProfiles ---

    [Fact]
    public void BuildProfiles_ShouldReturnProfileForEachDatabase_WhenTimeSeriesProvided()
    {
        // Arrange
        var ts = CreateTimeSeries(
            ("db1", [10.0, 20.0, 30.0, 40.0, 50.0]),
            ("db2", [50.0, 40.0, 30.0, 20.0, 10.0]));

        // Act
        var result = sut.BuildProfiles(ts);

        // Assert
        result.Should().HaveCount(2);
        result.Select(p => p.DatabaseName).Should().BeEquivalentTo(["db1", "db2"]);
    }

    [Fact]
    public void BuildProfiles_ShouldComputeCorrectStatistics_WhenKnownValues()
    {
        // Arrange
        var ts = CreateTimeSeries(("db1", [10.0, 20.0, 30.0, 40.0, 50.0]));

        // Act
        var result = sut.BuildProfiles(ts);

        // Assert
        var profile = result.Single();
        profile.Mean.Should().Be(30.0);
        profile.Peak.Should().Be(50.0);
        profile.P99.Should().BeGreaterThan(40.0);
    }

    [Fact]
    public void BuildProfiles_ShouldClassifyAsLowSignal_WhenFlatlinedSeries()
    {
        // Arrange -- near-zero series with tiny variance
        var ts = CreateTimeSeries(("flat-db", [0.0, 0.0, 0.0, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0]));
        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.BuildProfiles(ts, options);

        // Assert
        var profile = result.Single();
        profile.IsLowSignal.Should().BeTrue();
        profile.StdDev.Should().BeLessThan(1.5);
        profile.ActiveFraction.Should().BeLessThan(0.05);
    }

    [Fact]
    public void BuildProfiles_ShouldNotClassifyAsLowSignal_WhenHighP99()
    {
        // Arrange -- series with meaningful load
        var ts = CreateTimeSeries(("busy-db", [10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0, 100.0]));
        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.BuildProfiles(ts, options);

        // Assert
        result.Single().IsLowSignal.Should().BeFalse();
    }

    [Fact]
    public void BuildProfiles_ShouldComputeStdDevAndActiveFraction_WhenProfilesBuilt()
    {
        // Arrange
        var ts = CreateTimeSeries(("db1", [0.0, 0.0, 5.0, 0.0, 0.0]));
        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.BuildProfiles(ts, options);

        // Assert
        var profile = result.Single();
        profile.StdDev.Should().BeGreaterThan(0);
        profile.ActiveFraction.Should().BeGreaterThan(0);
    }

    // --- ComputePairwise ---

    [Fact]
    public void ComputePairwise_ShouldReturnHighBadScore_WhenPerfectlyCorrelated()
    {
        // Arrange — identical patterns
        var a = BuildProfile("db1", [10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0, 100.0]);
        var b = BuildProfile("db2", [10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0, 100.0]);

        // Act
        var result = sut.ComputePairwise(a, b, 0.90);

        // Assert
        result.PearsonCorrelation.Should().BeApproximately(1.0, 0.01);
        result.BadTogetherScore.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void ComputePairwise_ShouldReturnLowBadScore_WhenAntiCorrelated()
    {
        // Arrange — opposite patterns
        var a = BuildProfile("db1", [10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0, 100.0]);
        var b = BuildProfile("db2", [100.0, 90.0, 80.0, 70.0, 60.0, 50.0, 40.0, 30.0, 20.0, 10.0]);

        // Act
        var result = sut.ComputePairwise(a, b, 0.90);

        // Assert
        result.PearsonCorrelation.Should().BeApproximately(-1.0, 0.01);
        result.BadTogetherScore.Should().BeLessThan(0.5);
    }

    [Fact]
    public void ComputePairwise_ShouldReturnLowPeakOverlap_WhenPeaksDontOverlap()
    {
        // Arrange — db1 spikes at start, db2 spikes at end
        var a = BuildProfile("db1", [100.0, 90.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0]);
        var b = BuildProfile("db2", [10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 90.0, 100.0]);

        // Act
        var result = sut.ComputePairwise(a, b, 0.80);

        // Assert
        result.PeakOverlapFraction.Should().BeLessThan(0.3);
    }

    private DatabaseProfile BuildProfile(string name, double[] values)
    {
        return new DatabaseProfile(
            name,
            values,
            statisticsService.Mean(values),
            statisticsService.Percentile(values, 0.95),
            statisticsService.Percentile(values, 0.99),
            values.Max());
    }

    private static DtuTimeSeries CreateTimeSeries(params (string Name, double[] Values)[] databases)
    {
        var length = databases[0].Values.Length;
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timestamps = Enumerable.Range(0, length)
            .Select(i => baseTime.AddMinutes(i * 5))
            .ToList();

        var dict = databases.ToDictionary(
            d => d.Name,
            d => (IReadOnlyList<double>)d.Values);

        return new DtuTimeSeries(timestamps, dict);
    }
}
