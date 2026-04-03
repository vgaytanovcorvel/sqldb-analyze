using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class PoolabilityMetricsTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange & Act
        var metrics = new PoolabilityMetrics("DbA", "DbB", 0.85, 0.70, 0.30, 0.55);

        // Assert
        metrics.DatabaseA.Should().Be("DbA");
        metrics.DatabaseB.Should().Be("DbB");
        metrics.PearsonCorrelation.Should().Be(0.85);
        metrics.PeakCorrelation.Should().Be(0.70);
        metrics.PeakOverlapFraction.Should().Be(0.30);
        metrics.BadTogetherScore.Should().Be(0.55);
    }

    [Fact]
    public void Equality_Should_BeTrue_When_RecordsHaveSameValues()
    {
        // Arrange
        var m1 = new PoolabilityMetrics("A", "B", 0.5, 0.3, 0.1, 0.2);
        var m2 = new PoolabilityMetrics("A", "B", 0.5, 0.3, 0.1, 0.2);

        // Act & Assert
        m1.Should().Be(m2);
    }

    [Fact]
    public void Equality_Should_BeFalse_When_DatabaseADiffers()
    {
        // Arrange
        var m1 = new PoolabilityMetrics("A", "B", 0.5, 0.3, 0.1, 0.2);
        var m2 = new PoolabilityMetrics("C", "B", 0.5, 0.3, 0.1, 0.2);

        // Act & Assert
        m1.Should().NotBe(m2);
    }
}
