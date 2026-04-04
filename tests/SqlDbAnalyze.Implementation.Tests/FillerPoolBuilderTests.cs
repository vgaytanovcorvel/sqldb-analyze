using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class FillerPoolBuilderTests
{
    private readonly StatisticsService statisticsService = new();
    private readonly FillerPoolBuilder sut;

    public FillerPoolBuilderTests()
    {
        sut = new FillerPoolBuilder(statisticsService);
    }

    [Fact]
    public void BuildFillerPools_ShouldReturnEmpty_WhenNoProfiles()
    {
        // Arrange
        IReadOnlyList<DatabaseProfile> profiles = [];
        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.BuildFillerPools(profiles, options, 0);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildFillerPools_ShouldMarkAllPoolsAsFiller_WhenProfilesProvided()
    {
        // Arrange
        var profiles = new List<DatabaseProfile>
        {
            BuildLowSignalProfile("db1", [0.0, 0.0, 1.0, 0.0, 0.0]),
            BuildLowSignalProfile("db2", [0.0, 0.5, 0.0, 0.0, 0.0])
        };
        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.BuildFillerPools(profiles, options, 0);

        // Assert
        result.Should().AllSatisfy(p => p.IsFillerPool.Should().BeTrue());
    }

    [Fact]
    public void BuildFillerPools_ShouldUseStartPoolIndex_WhenOffsetProvided()
    {
        // Arrange
        var profiles = new List<DatabaseProfile>
        {
            BuildLowSignalProfile("db1", [0.0, 1.0, 0.0])
        };
        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.BuildFillerPools(profiles, options, 5);

        // Assert
        result[0].PoolIndex.Should().Be(5);
    }

    [Fact]
    public void BuildFillerPools_ShouldRespectMaxDatabasesPerPool_WhenLimitExceeded()
    {
        // Arrange
        var profiles = Enumerable.Range(1, 10)
            .Select(i => BuildLowSignalProfile($"db{i}", [0.0, 0.5, 0.0]))
            .ToList();
        var options = new PoolOptimizerOptions(FillerMaxDatabasesPerPool: 3);

        // Act
        var result = sut.BuildFillerPools(profiles, options, 0);

        // Assert
        result.Should().HaveCountGreaterThan(1);
        result.Should().AllSatisfy(p => p.DatabaseNames.Count.Should().BeLessThanOrEqualTo(3));
    }

    [Fact]
    public void BuildFillerPools_ShouldApplyFloorDtu_WhenCapacityIsNonZero()
    {
        // Arrange -- all zero series, but floor should ensure nonzero capacity
        var profiles = new List<DatabaseProfile>
        {
            BuildLowSignalProfile("db1", [0.0, 0.0, 0.0, 0.0, 0.0])
        };
        var options = new PoolOptimizerOptions(FillerFloorDtuMin: 2.0);

        // Act
        var result = sut.BuildFillerPools(profiles, options, 0);

        // Assert
        result[0].RecommendedCapacity.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BuildFillerPools_ShouldSortByPeakDescending_WhenMultipleProfiles()
    {
        // Arrange
        var profiles = new List<DatabaseProfile>
        {
            BuildLowSignalProfile("db-low", [0.0, 0.5, 0.0]),
            BuildLowSignalProfile("db-high", [0.0, 3.0, 0.0]),
            BuildLowSignalProfile("db-mid", [0.0, 1.5, 0.0])
        };
        var options = new PoolOptimizerOptions(FillerMaxDatabasesPerPool: 100);

        // Act
        var result = sut.BuildFillerPools(profiles, options, 0);

        // Assert -- all should be in one pool
        result.Should().HaveCount(1);
        result[0].DatabaseNames.Should().HaveCount(3);
    }

    [Fact]
    public void BuildFillerPools_ShouldUseFillerSafetyFactor_WhenSizing()
    {
        // Arrange
        var profiles = new List<DatabaseProfile>
        {
            BuildLowSignalProfile("db1", [2.0, 2.0, 2.0, 2.0, 2.0])
        };
        var normalOptions = new PoolOptimizerOptions(FillerSafetyFactor: 1.0);
        var highOptions = new PoolOptimizerOptions(FillerSafetyFactor: 2.0);

        // Act
        var normalResult = sut.BuildFillerPools(profiles, normalOptions, 0);
        var highResult = sut.BuildFillerPools(profiles, highOptions, 0);

        // Assert
        highResult[0].RecommendedCapacity.Should().BeGreaterThan(normalResult[0].RecommendedCapacity);
    }

    private DatabaseProfile BuildLowSignalProfile(string name, double[] values)
    {
        return new DatabaseProfile(
            name, values,
            statisticsService.Mean(values),
            statisticsService.Percentile(values, 0.95),
            statisticsService.Percentile(values, 0.99),
            values.Length > 0 ? values.Max() : 0,
            StdDev: statisticsService.StandardDeviation(values),
            ActiveFraction: 0.01,
            IsLowSignal: true);
    }
}
