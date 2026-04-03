using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class PlacementScoreTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange & Act
        var score = new PlacementScore(0, 15.5, 0.3, 0.0, 18.5);

        // Assert
        score.PoolIndex.Should().Be(0);
        score.CapacityIncrease.Should().Be(15.5);
        score.PairwisePenalty.Should().Be(0.3);
        score.OverloadPenalty.Should().Be(0.0);
        score.TotalScore.Should().Be(18.5);
    }

    [Fact]
    public void Equality_Should_BeTrue_When_RecordsHaveSameValues()
    {
        // Arrange
        var s1 = new PlacementScore(0, 10.0, 0.5, 0.0, 15.0);
        var s2 = new PlacementScore(0, 10.0, 0.5, 0.0, 15.0);

        // Act & Assert
        s1.Should().Be(s2);
    }

    [Fact]
    public void Equality_Should_BeFalse_When_TotalScoreDiffers()
    {
        // Arrange
        var s1 = new PlacementScore(0, 10.0, 0.5, 0.0, 15.0);
        var s2 = new PlacementScore(0, 10.0, 0.5, 0.0, 20.0);

        // Act & Assert
        s1.Should().NotBe(s2);
    }
}
