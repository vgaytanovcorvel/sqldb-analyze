using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class DatabaseInfoTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange & Act
        var info = new DatabaseInfo("TestDb", 1024.5, 100, "pool-1");

        // Assert
        info.DatabaseName.Should().Be("TestDb");
        info.DataSizeMB.Should().Be(1024.5);
        info.DtuLimit.Should().Be(100);
        info.ElasticPoolName.Should().Be("pool-1");
    }

    [Fact]
    public void Constructor_Should_AcceptNullElasticPoolName_When_NotInPool()
    {
        // Arrange & Act
        var info = new DatabaseInfo("TestDb", 512.0, 50, null);

        // Assert
        info.ElasticPoolName.Should().BeNull();
    }

    [Fact]
    public void Equality_Should_BeTrue_When_RecordsHaveSameValues()
    {
        // Arrange
        var info1 = new DatabaseInfo("Db", 100.0, 50, "pool");
        var info2 = new DatabaseInfo("Db", 100.0, 50, "pool");

        // Act & Assert
        info1.Should().Be(info2);
    }

    [Fact]
    public void Equality_Should_BeFalse_When_DatabaseNameDiffers()
    {
        // Arrange
        var info1 = new DatabaseInfo("Db1", 100.0, 50, null);
        var info2 = new DatabaseInfo("Db2", 100.0, 50, null);

        // Act & Assert
        info1.Should().NotBe(info2);
    }

    [Fact]
    public void WithExpression_Should_CreateModifiedCopy_When_PropertyChanged()
    {
        // Arrange
        var original = new DatabaseInfo("Db", 100.0, 50, null);

        // Act
        var modified = original with { ElasticPoolName = "new-pool" };

        // Assert
        modified.ElasticPoolName.Should().Be("new-pool");
        original.ElasticPoolName.Should().BeNull();
    }
}
