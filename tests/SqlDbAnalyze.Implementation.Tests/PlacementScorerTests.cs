using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class PlacementScorerTests
{
    private readonly StatisticsService statisticsService = new();
    private readonly PoolabilityService poolabilityService;
    private readonly PlacementScorer sut;

    public PlacementScorerTests()
    {
        poolabilityService = new PoolabilityService(statisticsService);
        sut = new PlacementScorer(statisticsService, poolabilityService);
    }

    [Fact]
    public void ScorePlacement_ShouldReturnScore_WhenAddingToEmptyPool()
    {
        // Arrange
        var db = BuildProfile("db1", [10.0, 20.0, 30.0, 40.0, 50.0]);
        IReadOnlyList<string> poolNames = [];
        IReadOnlyList<DatabaseProfile> poolMembers = [];
        IReadOnlyList<double> poolLoad = [0.0, 0.0, 0.0, 0.0, 0.0];
        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.ScorePlacement(db, poolNames, poolMembers, poolLoad, 0, options);

        // Assert
        result.PoolIndex.Should().Be(0);
        result.PairwisePenalty.Should().Be(0);
    }

    [Fact]
    public void ScorePlacement_ShouldHaveLowerScore_WhenAntiCorrelatedDatabaseAdded()
    {
        // Arrange
        var dbExisting = BuildProfile("db1", [100.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0]);
        var dbAntiCorrelated = BuildProfile("db2", [10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 100.0]);
        var dbCorrelated = BuildProfile("db3", [100.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0]);

        IReadOnlyList<string> poolNames = ["db1"];
        IReadOnlyList<DatabaseProfile> poolMembers = [dbExisting];
        IReadOnlyList<double> poolLoad = dbExisting.DtuValues;
        var options = new PoolOptimizerOptions();

        // Act
        var antiScore = sut.ScorePlacement(dbAntiCorrelated, poolNames, poolMembers, poolLoad, 0, options);
        var corrScore = sut.ScorePlacement(dbCorrelated, poolNames, poolMembers, poolLoad, 0, options);

        // Assert -- anti-correlated should have lower (better) score
        antiScore.TotalScore.Should().BeLessThan(corrScore.TotalScore);
    }

    [Fact]
    public void ScorePlacement_ShouldHaveZeroPairwisePenalty_WhenPoolIsEmpty()
    {
        // Arrange
        var db = BuildProfile("db1", [10.0, 20.0, 30.0]);
        IReadOnlyList<double> poolLoad = [0.0, 0.0, 0.0];
        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.ScorePlacement(db, [], [], poolLoad, 0, options);

        // Assert
        result.PairwisePenalty.Should().Be(0);
    }

    [Fact]
    public void ScorePlacement_ShouldIncludeOverloadPenalty_WhenCapacityExceeded()
    {
        // Arrange -- pool with very high load, adding another high-load DB
        var dbHigh = BuildProfile("db1", [1000.0, 1000.0, 1000.0]);
        var dbNew = BuildProfile("db2", [1000.0, 1000.0, 1000.0]);
        IReadOnlyList<double> poolLoad = dbHigh.DtuValues;
        var options = new PoolOptimizerOptions(MaxPoolCapacity: 100.0);

        // Act
        var result = sut.ScorePlacement(dbNew, ["db1"], [dbHigh], poolLoad, 0, options);

        // Assert
        result.OverloadPenalty.Should().BeGreaterThan(0);
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
}
