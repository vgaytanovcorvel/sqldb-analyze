using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class DtuTimeSeriesTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange
        var timestamps = new List<DateTimeOffset> { DateTimeOffset.UtcNow };
        var values = new Dictionary<string, IReadOnlyList<double>>
        {
            ["db1"] = new double[] { 50.0 }
        };

        // Act
        var ts = new DtuTimeSeries(timestamps, values);

        // Assert
        ts.Timestamps.Should().HaveCount(1);
        ts.DatabaseValues.Should().ContainKey("db1");
    }

    [Fact]
    public void Constructor_Should_AcceptEmpty_When_NoData()
    {
        // Arrange & Act
        var ts = new DtuTimeSeries(
            Array.Empty<DateTimeOffset>(),
            new Dictionary<string, IReadOnlyList<double>>());

        // Assert
        ts.Timestamps.Should().BeEmpty();
        ts.DatabaseValues.Should().BeEmpty();
    }

    [Fact]
    public void Equality_Should_BeTrue_When_SameReferences()
    {
        // Arrange
        IReadOnlyList<DateTimeOffset> timestamps = new List<DateTimeOffset>();
        IReadOnlyDictionary<string, IReadOnlyList<double>> values = new Dictionary<string, IReadOnlyList<double>>();
        var ts1 = new DtuTimeSeries(timestamps, values);
        var ts2 = new DtuTimeSeries(timestamps, values);

        // Act & Assert
        ts1.Should().Be(ts2);
    }
}
