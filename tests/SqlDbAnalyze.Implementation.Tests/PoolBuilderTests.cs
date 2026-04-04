using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class PoolBuilderTests
{
    private readonly StatisticsService statisticsService = new();
    private readonly PoolBuilder sut;

    public PoolBuilderTests()
    {
        var poolabilityService = new PoolabilityService(statisticsService);
        var placementScorer = new PlacementScorer(statisticsService, poolabilityService);
        var fillerPoolBuilder = new FillerPoolBuilder(statisticsService);
        sut = new PoolBuilder(placementScorer, statisticsService, fillerPoolBuilder);
    }

    [Fact]
    public void BuildPools_ShouldReturnEmpty_WhenNoProfilesProvided()
    {
        // Arrange
        IReadOnlyList<DatabaseProfile> profiles = [];
        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.BuildPools(profiles, options);

        // Assert
        result.Pools.Should().BeEmpty();
        result.TotalRequiredCapacity.Should().Be(0);
    }

    [Fact]
    public void BuildPools_ShouldCreateSinglePool_WhenOneDatabase()
    {
        // Arrange
        var profiles = new List<DatabaseProfile>
        {
            BuildProfile("db1", [10.0, 20.0, 30.0, 40.0, 50.0])
        };
        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.BuildPools(profiles, options);

        // Assert
        result.Pools.Should().HaveCount(1);
        result.Pools[0].DatabaseNames.Should().Contain("db1");
    }

    [Fact]
    public void BuildPools_ShouldGroupAntiCorrelatedDatabases_WhenOppositePatterns()
    {
        // Arrange — db1 peaks at start, db2 peaks at end (anti-correlated)
        var profiles = new List<DatabaseProfile>
        {
            BuildProfile("db1", [100.0, 80.0, 20.0, 10.0, 5.0, 5.0, 5.0, 5.0, 5.0, 5.0]),
            BuildProfile("db2", [5.0, 5.0, 5.0, 5.0, 5.0, 10.0, 20.0, 80.0, 100.0, 100.0])
        };
        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.BuildPools(profiles, options);

        // Assert — anti-correlated DBs should land in the same pool
        result.Pools.Should().HaveCount(1);
        result.Pools[0].DatabaseNames.Should().HaveCount(2);
    }

    [Fact]
    public void BuildPools_ShouldIsolateDatabases_WhenIsolateOptionSpecified()
    {
        // Arrange
        var profiles = new List<DatabaseProfile>
        {
            BuildProfile("db1", [10.0, 20.0, 30.0]),
            BuildProfile("db-critical", [40.0, 50.0, 60.0]),
            BuildProfile("db3", [15.0, 25.0, 35.0])
        };
        var options = new PoolOptimizerOptions(IsolateDatabases: ["db-critical"]);

        // Act
        var result = sut.BuildPools(profiles, options);

        // Assert
        result.IsolatedDatabases.Should().Contain("db-critical");
        var criticalPool = result.Pools.First(p => p.DatabaseNames.Contains("db-critical"));
        criticalPool.DatabaseNames.Should().HaveCount(1);
    }

    [Fact]
    public void BuildPools_ShouldRespectMaxDatabasesPerPool_WhenLimitIsLow()
    {
        // Arrange
        var profiles = Enumerable.Range(1, 5)
            .Select(i => BuildProfile($"db{i}", [10.0 * i, 20.0 * i, 30.0 * i]))
            .ToList();
        var options = new PoolOptimizerOptions(MaxDatabasesPerPool: 2);

        // Act
        var result = sut.BuildPools(profiles, options);

        // Assert
        result.Pools.Should().AllSatisfy(p => p.DatabaseNames.Count.Should().BeLessThanOrEqualTo(2));
    }

    [Fact]
    public void BuildPools_ShouldComputeDiversificationRatio_WhenPoolHasMultipleDatabases()
    {
        // Arrange
        var profiles = new List<DatabaseProfile>
        {
            BuildProfile("db1", [100.0, 10.0, 10.0, 10.0, 10.0]),
            BuildProfile("db2", [10.0, 10.0, 10.0, 10.0, 100.0])
        };
        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.BuildPools(profiles, options);

        // Assert — diversification ratio should be > 1 for anti-correlated workloads
        var pool = result.Pools.First(p => p.DatabaseNames.Count > 1);
        pool.DiversificationRatio.Should().BeGreaterThan(1.0);
    }

    [Fact]
    public void BuildPools_ShouldSeparateLowSignalIntoFillerPools_WhenMixedProfiles()
    {
        // Arrange -- db1/db2 are regular, db3/db4 are low-signal
        var profiles = new List<DatabaseProfile>
        {
            BuildProfile("db1", [100.0, 80.0, 60.0, 40.0, 20.0]),
            BuildProfile("db2", [20.0, 40.0, 60.0, 80.0, 100.0]),
            BuildLowSignalProfile("db3", [0.0, 0.0, 1.0, 0.0, 0.0]),
            BuildLowSignalProfile("db4", [0.0, 0.5, 0.0, 0.0, 0.0])
        };
        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.BuildPools(profiles, options);

        // Assert
        var fillerPools = result.Pools.Where(p => p.IsFillerPool).ToList();
        var regularPools = result.Pools.Where(p => !p.IsFillerPool).ToList();

        fillerPools.Should().NotBeEmpty();
        regularPools.Should().NotBeEmpty();
        fillerPools.SelectMany(p => p.DatabaseNames).Should().BeEquivalentTo(["db3", "db4"]);
    }

    [Fact]
    public void BuildPools_ShouldHandleAllLowSignal_WhenNoRegularDatabases()
    {
        // Arrange
        var profiles = new List<DatabaseProfile>
        {
            BuildLowSignalProfile("db1", [0.0, 0.0, 1.0, 0.0, 0.0]),
            BuildLowSignalProfile("db2", [0.0, 0.5, 0.0, 0.0, 0.0])
        };
        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.BuildPools(profiles, options);

        // Assert
        result.Pools.Should().AllSatisfy(p => p.IsFillerPool.Should().BeTrue());
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
