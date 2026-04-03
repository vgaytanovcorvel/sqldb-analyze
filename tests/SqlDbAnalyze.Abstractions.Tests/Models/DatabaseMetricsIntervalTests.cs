using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class DatabaseMetricsIntervalTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange
        var dbName = "TestDb";
        var earliest = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var latest = new DateTimeOffset(2025, 1, 7, 0, 0, 0, TimeSpan.Zero);
        var count = 2016;

        // Act
        var interval = new DatabaseMetricsInterval(dbName, earliest, latest, count);

        // Assert
        interval.DatabaseName.Should().Be(dbName);
        interval.EarliestTimestamp.Should().Be(earliest);
        interval.LatestTimestamp.Should().Be(latest);
        interval.MetricCount.Should().Be(count);
    }

    [Fact]
    public void Constructor_Should_AcceptNullTimestamps_When_NoDataAvailable()
    {
        // Arrange & Act
        var interval = new DatabaseMetricsInterval("TestDb", null, null, 0);

        // Assert
        interval.EarliestTimestamp.Should().BeNull();
        interval.LatestTimestamp.Should().BeNull();
        interval.MetricCount.Should().Be(0);
    }

    [Fact]
    public void Equality_Should_BeTrue_When_RecordsHaveSameValues()
    {
        // Arrange
        var earliest = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var interval1 = new DatabaseMetricsInterval("Db", earliest, earliest, 100);
        var interval2 = new DatabaseMetricsInterval("Db", earliest, earliest, 100);

        // Act & Assert
        interval1.Should().Be(interval2);
    }
}
