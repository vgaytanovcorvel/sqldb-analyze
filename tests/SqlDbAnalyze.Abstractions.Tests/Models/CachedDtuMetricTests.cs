using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class CachedDtuMetricTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange
        var id = 42L;
        var serverId = 1;
        var dbName = "TestDb";
        var timestamp = DateTimeOffset.UtcNow;
        var dtu = 85.5;

        // Act
        var metric = new CachedDtuMetric(id, serverId, dbName, timestamp, dtu);

        // Assert
        metric.CachedDtuMetricId.Should().Be(id);
        metric.RegisteredServerId.Should().Be(serverId);
        metric.DatabaseName.Should().Be(dbName);
        metric.Timestamp.Should().Be(timestamp);
        metric.DtuPercentage.Should().Be(dtu);
    }

    [Fact]
    public void Equality_Should_BeTrue_When_RecordsHaveSameValues()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var metric1 = new CachedDtuMetric(1, 1, "Db", timestamp, 50.0);
        var metric2 = new CachedDtuMetric(1, 1, "Db", timestamp, 50.0);

        // Act & Assert
        metric1.Should().Be(metric2);
    }

    [Fact]
    public void Equality_Should_BeFalse_When_DtuPercentageDiffers()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var metric1 = new CachedDtuMetric(1, 1, "Db", timestamp, 50.0);
        var metric2 = new CachedDtuMetric(1, 1, "Db", timestamp, 75.0);

        // Act & Assert
        metric1.Should().NotBe(metric2);
    }
}
