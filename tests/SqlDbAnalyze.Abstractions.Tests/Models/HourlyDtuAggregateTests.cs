using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class HourlyDtuAggregateTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange
        var hour = new DateTimeOffset(2025, 6, 15, 14, 0, 0, TimeSpan.Zero);

        // Act
        var aggregate = new HourlyDtuAggregate(hour, 55.5, 88.8);

        // Assert
        aggregate.Hour.Should().Be(hour);
        aggregate.AverageDtuPercent.Should().Be(55.5);
        aggregate.MaxDtuPercent.Should().Be(88.8);
    }

    [Fact]
    public void Equality_Should_BeTrue_When_RecordsHaveSameValues()
    {
        // Arrange
        var hour = new DateTimeOffset(2025, 6, 15, 14, 0, 0, TimeSpan.Zero);
        var aggregate1 = new HourlyDtuAggregate(hour, 55.5, 88.8);
        var aggregate2 = new HourlyDtuAggregate(hour, 55.5, 88.8);

        // Act
        var areEqual = aggregate1 == aggregate2;

        // Assert
        aggregate1.Should().Be(aggregate2);
        areEqual.Should().BeTrue();
        aggregate1.GetHashCode().Should().Be(aggregate2.GetHashCode());
    }

    [Fact]
    public void Equality_Should_BeFalse_When_HourDiffers()
    {
        // Arrange
        var hour1 = new DateTimeOffset(2025, 6, 15, 14, 0, 0, TimeSpan.Zero);
        var hour2 = new DateTimeOffset(2025, 6, 15, 15, 0, 0, TimeSpan.Zero);
        var aggregate1 = new HourlyDtuAggregate(hour1, 55.5, 88.8);
        var aggregate2 = new HourlyDtuAggregate(hour2, 55.5, 88.8);

        // Act
        var areNotEqual = aggregate1 != aggregate2;

        // Assert
        aggregate1.Should().NotBe(aggregate2);
        areNotEqual.Should().BeTrue();
    }

    [Fact]
    public void Equality_Should_BeFalse_When_AverageDtuPercentDiffers()
    {
        // Arrange
        var hour = new DateTimeOffset(2025, 6, 15, 14, 0, 0, TimeSpan.Zero);
        var aggregate1 = new HourlyDtuAggregate(hour, 55.5, 88.8);
        var aggregate2 = new HourlyDtuAggregate(hour, 60.0, 88.8);

        // Act
        var areNotEqual = aggregate1 != aggregate2;

        // Assert
        aggregate1.Should().NotBe(aggregate2);
        areNotEqual.Should().BeTrue();
    }

    [Fact]
    public void Equality_Should_BeFalse_When_MaxDtuPercentDiffers()
    {
        // Arrange
        var hour = new DateTimeOffset(2025, 6, 15, 14, 0, 0, TimeSpan.Zero);
        var aggregate1 = new HourlyDtuAggregate(hour, 55.5, 88.8);
        var aggregate2 = new HourlyDtuAggregate(hour, 55.5, 95.0);

        // Act
        var areNotEqual = aggregate1 != aggregate2;

        // Assert
        aggregate1.Should().NotBe(aggregate2);
        areNotEqual.Should().BeTrue();
    }

    [Fact]
    public void WithExpression_Should_CreateModifiedCopy_When_PropertyChanged()
    {
        // Arrange
        var hour = new DateTimeOffset(2025, 6, 15, 14, 0, 0, TimeSpan.Zero);
        var original = new HourlyDtuAggregate(hour, 55.5, 88.8);

        // Act
        var modified = original with { MaxDtuPercent = 100.0 };

        // Assert
        modified.MaxDtuPercent.Should().Be(100.0);
        modified.Hour.Should().Be(original.Hour);
        modified.AverageDtuPercent.Should().Be(original.AverageDtuPercent);
    }

    [Fact]
    public void WithExpression_Should_PreserveOriginal_When_CopyCreated()
    {
        // Arrange
        var hour = new DateTimeOffset(2025, 6, 15, 14, 0, 0, TimeSpan.Zero);
        var original = new HourlyDtuAggregate(hour, 55.5, 88.8);

        // Act
        _ = original with { AverageDtuPercent = 99.9 };

        // Assert
        original.AverageDtuPercent.Should().Be(55.5);
    }

    [Fact]
    public void Constructor_Should_AcceptZeroValues_When_ZerosProvided()
    {
        // Arrange
        var hour = DateTimeOffset.MinValue;

        // Act
        var aggregate = new HourlyDtuAggregate(hour, 0.0, 0.0);

        // Assert
        aggregate.AverageDtuPercent.Should().Be(0.0);
        aggregate.MaxDtuPercent.Should().Be(0.0);
    }

    [Fact]
    public void ToString_Should_ContainPropertyValues_When_Called()
    {
        // Arrange
        var hour = new DateTimeOffset(2025, 6, 15, 14, 0, 0, TimeSpan.Zero);
        var aggregate = new HourlyDtuAggregate(hour, 55.5, 88.8);

        // Act
        var result = aggregate.ToString();

        // Assert
        result.Should().Contain("55.5");
        result.Should().Contain("88.8");
    }
}
