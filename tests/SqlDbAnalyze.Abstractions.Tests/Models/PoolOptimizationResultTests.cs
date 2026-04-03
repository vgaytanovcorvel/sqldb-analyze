using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class PoolOptimizationResultTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange
        var pools = new List<PoolAssignment>
        {
            new(0, new List<string> { "db1" }, 50.0, 40.0, 45.0, 50.0, 1.0, 0.0)
        };
        var isolated = new List<string> { "db-critical" };

        // Act
        var result = new PoolOptimizationResult(pools, 50.0, isolated);

        // Assert
        result.Pools.Should().HaveCount(1);
        result.TotalRequiredCapacity.Should().Be(50.0);
        result.IsolatedDatabases.Should().Contain("db-critical");
    }

    [Fact]
    public void Constructor_Should_AcceptEmpty_When_NoPools()
    {
        // Arrange & Act
        var result = new PoolOptimizationResult(
            new List<PoolAssignment>(), 0.0, new List<string>());

        // Assert
        result.Pools.Should().BeEmpty();
        result.TotalRequiredCapacity.Should().Be(0.0);
        result.IsolatedDatabases.Should().BeEmpty();
    }

    [Fact]
    public void WithExpression_Should_UpdateCapacity_When_Changed()
    {
        // Arrange
        var result = new PoolOptimizationResult(
            new List<PoolAssignment>(), 100.0, new List<string>());

        // Act
        var modified = result with { TotalRequiredCapacity = 200.0 };

        // Assert
        modified.TotalRequiredCapacity.Should().Be(200.0);
        result.TotalRequiredCapacity.Should().Be(100.0);
    }
}
