using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class LocalSearchOptimizerTests
{
    private readonly StatisticsService statisticsService = new();
    private readonly LocalSearchOptimizer sut;

    public LocalSearchOptimizerTests()
    {
        sut = new LocalSearchOptimizer(statisticsService);
    }

    [Fact]
    public void Improve_ShouldReturnSameResult_WhenSinglePool()
    {
        // Arrange
        var profiles = new List<DatabaseProfile>
        {
            BuildProfile("db1", [10.0, 20.0, 30.0])
        };

        var initial = new PoolOptimizationResult(
            [new PoolAssignment(0, ["db1"], 33.0, 28.5, 30.0, 30.0, 1.0, 0)],
            33.0, []);

        var options = new PoolOptimizerOptions();

        // Act
        var result = sut.Improve(initial, profiles, options);

        // Assert
        result.Pools.Should().HaveCount(1);
    }

    [Fact]
    public void Improve_ShouldReduceTotalCapacity_WhenMisplacedDatabaseExists()
    {
        // Arrange — db2 is anti-correlated with db1 but placed in separate pool
        var profiles = new List<DatabaseProfile>
        {
            BuildProfile("db1", [100.0, 10.0, 10.0, 10.0, 10.0]),
            BuildProfile("db2", [10.0, 10.0, 10.0, 10.0, 100.0]),
            BuildProfile("db3", [50.0, 50.0, 50.0, 50.0, 50.0])
        };

        // Deliberately misplace: db1 alone, db2+db3 together
        var pool0 = BuildAssignment(0, ["db1"], profiles);
        var pool1 = BuildAssignment(1, ["db2", "db3"], profiles);

        var initial = new PoolOptimizationResult(
            [pool0, pool1],
            pool0.RecommendedCapacity + pool1.RecommendedCapacity,
            []);

        var options = new PoolOptimizerOptions(MaxSearchPasses: 5);

        // Act
        var result = sut.Improve(initial, profiles, options);

        // Assert
        result.TotalRequiredCapacity.Should().BeLessThanOrEqualTo(initial.TotalRequiredCapacity);
    }

    [Fact]
    public void Improve_ShouldRespectMaxSearchPasses_WhenSetToZero()
    {
        // Arrange
        var profiles = new List<DatabaseProfile>
        {
            BuildProfile("db1", [10.0, 20.0]),
            BuildProfile("db2", [20.0, 10.0])
        };

        var pool0 = BuildAssignment(0, ["db1"], profiles);
        var pool1 = BuildAssignment(1, ["db2"], profiles);

        var initial = new PoolOptimizationResult(
            [pool0, pool1],
            pool0.RecommendedCapacity + pool1.RecommendedCapacity,
            []);

        var options = new PoolOptimizerOptions(MaxSearchPasses: 0);

        // Act
        var result = sut.Improve(initial, profiles, options);

        // Assert — with 0 passes, no improvement should happen
        result.TotalRequiredCapacity.Should().Be(initial.TotalRequiredCapacity);
    }

    private DatabaseProfile BuildProfile(string name, double[] values)
    {
        return new DatabaseProfile(
            name, values,
            statisticsService.Mean(values),
            statisticsService.Percentile(values, 0.95),
            statisticsService.Percentile(values, 0.99),
            values.Max());
    }

    private PoolAssignment BuildAssignment(
        int index,
        List<string> dbNames,
        List<DatabaseProfile> allProfiles)
    {
        var series = dbNames
            .Select(n => allProfiles.First(p => p.DatabaseName == n).DtuValues)
            .ToList();
        var combined = statisticsService.SumSeries(series);
        var capacity = statisticsService.Percentile(combined, 0.99) * 1.10;

        return new PoolAssignment(
            index, dbNames, capacity,
            statisticsService.Percentile(combined, 0.95),
            statisticsService.Percentile(combined, 0.99),
            combined.Max(),
            1.0, 0);
    }
}
