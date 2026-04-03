using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class PoolSimulationRequestTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange
        var dbNames = new List<string> { "db1", "db2" };
        var limits = new Dictionary<string, int> { ["db1"] = 100, ["db2"] = 200 };

        // Act
        var request = new PoolSimulationRequest(dbNames, limits);

        // Assert
        request.DatabaseNames.Should().HaveCount(2);
        request.DtuLimits.Should().ContainKey("db1");
        request.DtuLimits["db1"].Should().Be(100);
    }

    [Fact]
    public void Equality_Should_BeTrue_When_SameReferences()
    {
        // Arrange
        IReadOnlyList<string> names = new List<string> { "db1" };
        IReadOnlyDictionary<string, int> limits = new Dictionary<string, int> { ["db1"] = 50 };
        var r1 = new PoolSimulationRequest(names, limits);
        var r2 = new PoolSimulationRequest(names, limits);

        // Act & Assert
        r1.Should().Be(r2);
    }
}
