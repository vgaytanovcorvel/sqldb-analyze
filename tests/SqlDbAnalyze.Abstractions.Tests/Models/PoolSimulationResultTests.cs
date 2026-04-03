using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class PoolSimulationResultTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange
        var dbNames = new List<string> { "db1", "db2" };

        // Act
        var result = new PoolSimulationResult(
            dbNames, 80.0, 95.0, 110.0, 50.0, 1.5, 0.001, 104.5, 300.0, 65.0);

        // Assert
        result.DatabaseNames.Should().HaveCount(2);
        result.P95Dtu.Should().Be(80.0);
        result.P99Dtu.Should().Be(95.0);
        result.PeakDtu.Should().Be(110.0);
        result.MeanDtu.Should().Be(50.0);
        result.DiversificationRatio.Should().Be(1.5);
        result.OverloadFraction.Should().Be(0.001);
        result.RecommendedPoolDtu.Should().Be(104.5);
        result.SumIndividualDtuLimits.Should().Be(300.0);
        result.EstimatedSavingsPercent.Should().Be(65.0);
    }

    [Fact]
    public void Equality_Should_BeTrue_When_SameReferences()
    {
        // Arrange
        IReadOnlyList<string> names = new List<string> { "db1" };
        var r1 = new PoolSimulationResult(names, 50.0, 60.0, 70.0, 30.0, 1.2, 0.0, 66.0, 100.0, 34.0);
        var r2 = new PoolSimulationResult(names, 50.0, 60.0, 70.0, 30.0, 1.2, 0.0, 66.0, 100.0, 34.0);

        // Act & Assert
        r1.Should().Be(r2);
    }
}
