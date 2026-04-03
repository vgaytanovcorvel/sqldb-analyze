using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class PoolAssignmentTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange
        var dbNames = new List<string> { "db1", "db2" };

        // Act
        var assignment = new PoolAssignment(0, dbNames, 100.0, 80.0, 95.0, 110.0, 1.5, 0.001);

        // Assert
        assignment.PoolIndex.Should().Be(0);
        assignment.DatabaseNames.Should().HaveCount(2);
        assignment.RecommendedCapacity.Should().Be(100.0);
        assignment.P95Load.Should().Be(80.0);
        assignment.P99Load.Should().Be(95.0);
        assignment.PeakLoad.Should().Be(110.0);
        assignment.DiversificationRatio.Should().Be(1.5);
        assignment.OverloadFraction.Should().Be(0.001);
    }

    [Fact]
    public void Equality_Should_BeTrue_When_SameReferences()
    {
        // Arrange
        IReadOnlyList<string> names = new List<string> { "db1" };
        var a1 = new PoolAssignment(0, names, 50.0, 40.0, 45.0, 50.0, 1.0, 0.0);
        var a2 = new PoolAssignment(0, names, 50.0, 40.0, 45.0, 50.0, 1.0, 0.0);

        // Act & Assert
        a1.Should().Be(a2);
    }
}
