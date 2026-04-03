using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class PoolOptimizerOptionsTests
{
    [Fact]
    public void Constructor_Should_UseDefaults_When_NoArgumentsProvided()
    {
        // Arrange & Act
        var options = new PoolOptimizerOptions();

        // Assert
        options.TargetPercentile.Should().Be(0.99);
        options.SafetyFactor.Should().Be(1.10);
        options.MaxOverloadFraction.Should().Be(0.001);
        options.MaxDatabasesPerPool.Should().Be(50);
        options.PeakThreshold.Should().Be(0.90);
        options.MaxPoolCapacity.Should().BeNull();
        options.IsolateDatabases.Should().BeNull();
        options.MaxSearchPasses.Should().Be(10);
        options.MinDiversificationRatio.Should().Be(1.25);
    }

    [Fact]
    public void Constructor_Should_SetCustomValues_When_ArgumentsProvided()
    {
        // Arrange
        var isolate = new List<string> { "db-critical" };

        // Act
        var options = new PoolOptimizerOptions(
            TargetPercentile: 0.95,
            SafetyFactor: 1.20,
            MaxOverloadFraction: 0.01,
            MaxDatabasesPerPool: 25,
            PeakThreshold: 0.80,
            MaxPoolCapacity: 500.0,
            IsolateDatabases: isolate,
            MaxSearchPasses: 5,
            MinDiversificationRatio: 1.50);

        // Assert
        options.TargetPercentile.Should().Be(0.95);
        options.SafetyFactor.Should().Be(1.20);
        options.MaxOverloadFraction.Should().Be(0.01);
        options.MaxDatabasesPerPool.Should().Be(25);
        options.PeakThreshold.Should().Be(0.80);
        options.MaxPoolCapacity.Should().Be(500.0);
        options.IsolateDatabases.Should().Contain("db-critical");
        options.MaxSearchPasses.Should().Be(5);
        options.MinDiversificationRatio.Should().Be(1.50);
    }

    [Fact]
    public void Equality_Should_BeTrue_When_BothUseDefaults()
    {
        // Arrange
        var o1 = new PoolOptimizerOptions();
        var o2 = new PoolOptimizerOptions();

        // Act & Assert
        o1.Should().Be(o2);
    }
}
