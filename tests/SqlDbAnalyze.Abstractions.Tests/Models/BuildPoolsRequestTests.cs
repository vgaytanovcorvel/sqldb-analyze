using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class BuildPoolsRequestTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange
        var dbNames = new List<string> { "db1", "db2" };
        var dtuLimits = new Dictionary<string, int> { ["db1"] = 100, ["db2"] = 200 };

        // Act
        var request = new BuildPoolsRequest(dbNames, dtuLimits, 0.95, 1.20, 30, 1.50);

        // Assert
        request.DatabaseNames.Should().BeEquivalentTo(dbNames);
        request.DtuLimits.Should().BeEquivalentTo(dtuLimits);
        request.TargetPercentile.Should().Be(0.95);
        request.SafetyFactor.Should().Be(1.20);
        request.MaxDatabasesPerPool.Should().Be(30);
        request.MinDiversificationRatio.Should().Be(1.50);
    }

    [Fact]
    public void Constructor_Should_UseDefaults_When_OptionalParametersOmitted()
    {
        // Arrange
        var dbNames = new List<string> { "db1" };
        var dtuLimits = new Dictionary<string, int> { ["db1"] = 100 };

        // Act
        var request = new BuildPoolsRequest(dbNames, dtuLimits);

        // Assert
        request.TargetPercentile.Should().Be(0.99);
        request.SafetyFactor.Should().Be(1.10);
        request.MaxDatabasesPerPool.Should().Be(50);
        request.MinDiversificationRatio.Should().Be(1.25);
    }

    [Fact]
    public void Equality_Should_BeTrue_When_SameReferences()
    {
        // Arrange
        IReadOnlyList<string> dbNames = new List<string> { "db1" };
        IReadOnlyDictionary<string, int> dtuLimits = new Dictionary<string, int> { ["db1"] = 100 };
        var req1 = new BuildPoolsRequest(dbNames, dtuLimits);
        var req2 = new BuildPoolsRequest(dbNames, dtuLimits);

        // Act & Assert
        req1.Should().Be(req2);
    }
}
