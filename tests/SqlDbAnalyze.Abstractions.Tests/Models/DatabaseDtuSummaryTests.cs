using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class DatabaseDtuSummaryTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange
        var databaseName = "TestDb";
        var averageDtu = 45.5;
        var peakDtu = 92.3;
        var dtuLimit = 100;

        // Act
        var summary = new DatabaseDtuSummary(databaseName, averageDtu, peakDtu, dtuLimit);

        // Assert
        summary.DatabaseName.Should().Be(databaseName);
        summary.AverageDtuPercent.Should().Be(averageDtu);
        summary.PeakDtuPercent.Should().Be(peakDtu);
        summary.CurrentDtuLimit.Should().Be(dtuLimit);
    }

    [Fact]
    public void Equality_Should_BeTrue_When_RecordsHaveSameValues()
    {
        // Arrange
        var summary1 = new DatabaseDtuSummary("TestDb", 45.5, 92.3, 100);
        var summary2 = new DatabaseDtuSummary("TestDb", 45.5, 92.3, 100);

        // Act
        var areEqual = summary1 == summary2;

        // Assert
        summary1.Should().Be(summary2);
        areEqual.Should().BeTrue();
        summary1.GetHashCode().Should().Be(summary2.GetHashCode());
    }

    [Fact]
    public void Equality_Should_BeFalse_When_DatabaseNameDiffers()
    {
        // Arrange
        var summary1 = new DatabaseDtuSummary("TestDb", 45.5, 92.3, 100);
        var summary2 = new DatabaseDtuSummary("OtherDb", 45.5, 92.3, 100);

        // Act
        var areNotEqual = summary1 != summary2;

        // Assert
        summary1.Should().NotBe(summary2);
        areNotEqual.Should().BeTrue();
    }

    [Fact]
    public void Equality_Should_BeFalse_When_AverageDtuPercentDiffers()
    {
        // Arrange
        var summary1 = new DatabaseDtuSummary("TestDb", 45.5, 92.3, 100);
        var summary2 = new DatabaseDtuSummary("TestDb", 60.0, 92.3, 100);

        // Act
        var areNotEqual = summary1 != summary2;

        // Assert
        summary1.Should().NotBe(summary2);
        areNotEqual.Should().BeTrue();
    }

    [Fact]
    public void Equality_Should_BeFalse_When_PeakDtuPercentDiffers()
    {
        // Arrange
        var summary1 = new DatabaseDtuSummary("TestDb", 45.5, 92.3, 100);
        var summary2 = new DatabaseDtuSummary("TestDb", 45.5, 99.9, 100);

        // Act
        var areNotEqual = summary1 != summary2;

        // Assert
        summary1.Should().NotBe(summary2);
        areNotEqual.Should().BeTrue();
    }

    [Fact]
    public void Equality_Should_BeFalse_When_CurrentDtuLimitDiffers()
    {
        // Arrange
        var summary1 = new DatabaseDtuSummary("TestDb", 45.5, 92.3, 100);
        var summary2 = new DatabaseDtuSummary("TestDb", 45.5, 92.3, 200);

        // Act
        var areNotEqual = summary1 != summary2;

        // Assert
        summary1.Should().NotBe(summary2);
        areNotEqual.Should().BeTrue();
    }

    [Fact]
    public void WithExpression_Should_CreateModifiedCopy_When_PropertyChanged()
    {
        // Arrange
        var original = new DatabaseDtuSummary("TestDb", 45.5, 92.3, 100);

        // Act
        var modified = original with { CurrentDtuLimit = 200 };

        // Assert
        modified.CurrentDtuLimit.Should().Be(200);
        modified.DatabaseName.Should().Be(original.DatabaseName);
        modified.AverageDtuPercent.Should().Be(original.AverageDtuPercent);
        modified.PeakDtuPercent.Should().Be(original.PeakDtuPercent);
    }

    [Fact]
    public void WithExpression_Should_PreserveOriginal_When_CopyCreated()
    {
        // Arrange
        var original = new DatabaseDtuSummary("TestDb", 45.5, 92.3, 100);

        // Act
        _ = original with { AverageDtuPercent = 99.9 };

        // Assert
        original.AverageDtuPercent.Should().Be(45.5);
    }

    [Fact]
    public void Constructor_Should_AcceptZeroValues_When_ZerosProvided()
    {
        // Arrange
        var databaseName = "TestDb";

        // Act
        var summary = new DatabaseDtuSummary(databaseName, 0.0, 0.0, 0);

        // Assert
        summary.AverageDtuPercent.Should().Be(0.0);
        summary.PeakDtuPercent.Should().Be(0.0);
        summary.CurrentDtuLimit.Should().Be(0);
    }

    [Fact]
    public void ToString_Should_ContainPropertyValues_When_Called()
    {
        // Arrange
        var summary = new DatabaseDtuSummary("MyDatabase", 55.5, 88.8, 150);

        // Act
        var result = summary.ToString();

        // Assert
        result.Should().Contain("MyDatabase");
        result.Should().Contain("55.5");
        result.Should().Contain("88.8");
        result.Should().Contain("150");
    }
}
