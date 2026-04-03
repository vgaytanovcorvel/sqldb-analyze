using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class DatabaseProfileTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange
        var values = new double[] { 10.0, 20.0, 30.0 };

        // Act
        var profile = new DatabaseProfile("db1", values, 20.0, 28.0, 29.5, 30.0);

        // Assert
        profile.DatabaseName.Should().Be("db1");
        profile.DtuValues.Should().BeEquivalentTo(values);
        profile.Mean.Should().Be(20.0);
        profile.P95.Should().Be(28.0);
        profile.P99.Should().Be(29.5);
        profile.Peak.Should().Be(30.0);
    }

    [Fact]
    public void Equality_Should_BeTrue_When_SameReferences()
    {
        // Arrange
        IReadOnlyList<double> values = new double[] { 10.0 };
        var p1 = new DatabaseProfile("db1", values, 10.0, 10.0, 10.0, 10.0);
        var p2 = new DatabaseProfile("db1", values, 10.0, 10.0, 10.0, 10.0);

        // Act & Assert
        p1.Should().Be(p2);
    }

    [Fact]
    public void Equality_Should_BeFalse_When_NameDiffers()
    {
        // Arrange
        IReadOnlyList<double> values = new double[] { 10.0 };
        var p1 = new DatabaseProfile("db1", values, 10.0, 10.0, 10.0, 10.0);
        var p2 = new DatabaseProfile("db2", values, 10.0, 10.0, 10.0, 10.0);

        // Act & Assert
        p1.Should().NotBe(p2);
    }
}
