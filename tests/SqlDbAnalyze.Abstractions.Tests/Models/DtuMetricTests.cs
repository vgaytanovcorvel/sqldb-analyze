using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class DtuMetricTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange
        var databaseName = "TestDb";
        var timestamp = DateTimeOffset.UtcNow;
        var dtuPercentage = 75.5;

        // Act
        var metric = new DtuMetric(databaseName, timestamp, dtuPercentage);

        // Assert
        metric.DatabaseName.Should().Be(databaseName);
        metric.Timestamp.Should().Be(timestamp);
        metric.DtuPercentage.Should().Be(dtuPercentage);
    }

    [Fact]
    public void Equality_Should_BeTrue_When_RecordsHaveSameValues()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var metric1 = new DtuMetric("TestDb", timestamp, 50.0);
        var metric2 = new DtuMetric("TestDb", timestamp, 50.0);

        // Act
        var areEqual = metric1 == metric2;

        // Assert
        metric1.Should().Be(metric2);
        areEqual.Should().BeTrue();
        metric1.GetHashCode().Should().Be(metric2.GetHashCode());
    }

    [Fact]
    public void Equality_Should_BeFalse_When_DatabaseNameDiffers()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var metric1 = new DtuMetric("TestDb", timestamp, 50.0);
        var metric2 = new DtuMetric("OtherDb", timestamp, 50.0);

        // Act
        var areNotEqual = metric1 != metric2;

        // Assert
        metric1.Should().NotBe(metric2);
        areNotEqual.Should().BeTrue();
    }

    [Fact]
    public void Equality_Should_BeFalse_When_DtuPercentageDiffers()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var metric1 = new DtuMetric("TestDb", timestamp, 50.0);
        var metric2 = new DtuMetric("TestDb", timestamp, 75.0);

        // Act
        var areNotEqual = metric1 != metric2;

        // Assert
        metric1.Should().NotBe(metric2);
        areNotEqual.Should().BeTrue();
    }

    [Fact]
    public void Equality_Should_BeFalse_When_TimestampDiffers()
    {
        // Arrange
        var timestamp1 = new DateTimeOffset(2025, 6, 15, 14, 0, 0, TimeSpan.Zero);
        var timestamp2 = new DateTimeOffset(2025, 6, 15, 15, 0, 0, TimeSpan.Zero);
        var metric1 = new DtuMetric("TestDb", timestamp1, 50.0);
        var metric2 = new DtuMetric("TestDb", timestamp2, 50.0);

        // Act
        var areNotEqual = metric1 != metric2;

        // Assert
        metric1.Should().NotBe(metric2);
        areNotEqual.Should().BeTrue();
    }

    [Fact]
    public void WithExpression_Should_CreateModifiedCopy_When_PropertyChanged()
    {
        // Arrange
        var original = new DtuMetric("TestDb", DateTimeOffset.UtcNow, 50.0);

        // Act
        var modified = original with { DtuPercentage = 99.9 };

        // Assert
        modified.DtuPercentage.Should().Be(99.9);
        modified.DatabaseName.Should().Be(original.DatabaseName);
        modified.Timestamp.Should().Be(original.Timestamp);
        modified.Should().NotBe(original);
    }

    [Fact]
    public void WithExpression_Should_PreserveOriginal_When_CopyCreated()
    {
        // Arrange
        var original = new DtuMetric("TestDb", DateTimeOffset.UtcNow, 50.0);

        // Act
        _ = original with { DatabaseName = "Modified" };

        // Assert
        original.DatabaseName.Should().Be("TestDb");
    }

    [Fact]
    public void Constructor_Should_AcceptZeroDtuPercentage_When_ZeroProvided()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var metric = new DtuMetric("TestDb", timestamp, 0.0);

        // Assert
        metric.DtuPercentage.Should().Be(0.0);
    }

    [Fact]
    public void Constructor_Should_AcceptNegativeDtuPercentage_When_NegativeProvided()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var metric = new DtuMetric("TestDb", timestamp, -1.0);

        // Assert
        metric.DtuPercentage.Should().Be(-1.0);
    }

    [Fact]
    public void ToString_Should_ContainPropertyValues_When_Called()
    {
        // Arrange
        var metric = new DtuMetric("TestDb", DateTimeOffset.MinValue, 42.5);

        // Act
        var result = metric.ToString();

        // Assert
        result.Should().Contain("TestDb");
        result.Should().Contain("42.5");
    }
}
