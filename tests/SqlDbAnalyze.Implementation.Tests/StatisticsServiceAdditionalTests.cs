using FluentAssertions;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class StatisticsServiceAdditionalTests
{
    private readonly StatisticsService sut = new();

    // --- Mean edge cases ---

    [Fact]
    public void Mean_ShouldReturnCorrectValue_WhenAllValuesAreIdentical()
    {
        // Arrange
        IReadOnlyList<double> values = [42.0, 42.0, 42.0];

        // Act
        var result = sut.Mean(values);

        // Assert
        result.Should().Be(42.0);
    }

    [Fact]
    public void Mean_ShouldHandleNegativeValues_WhenMixed()
    {
        // Arrange
        IReadOnlyList<double> values = [-10.0, 10.0];

        // Act
        var result = sut.Mean(values);

        // Assert
        result.Should().Be(0.0);
    }

    // --- Percentile edge cases ---

    [Fact]
    public void Percentile_ShouldReturnValue_WhenSingleElement()
    {
        // Arrange
        IReadOnlyList<double> values = [42.0];

        // Act
        var result = sut.Percentile(values, 0.5);

        // Assert
        result.Should().Be(42.0);
    }

    [Fact]
    public void Percentile_ShouldReturnMin_WhenNegativePercentile()
    {
        // Arrange
        IReadOnlyList<double> values = [10.0, 20.0, 30.0];

        // Act
        var result = sut.Percentile(values, -0.5);

        // Assert
        result.Should().Be(10.0);
    }

    [Fact]
    public void Percentile_ShouldReturnMax_WhenPercentileAboveOne()
    {
        // Arrange
        IReadOnlyList<double> values = [10.0, 20.0, 30.0];

        // Act
        var result = sut.Percentile(values, 1.5);

        // Assert
        result.Should().Be(30.0);
    }

    // --- PearsonCorrelation edge cases ---

    [Fact]
    public void PearsonCorrelation_ShouldReturnZero_WhenEmptyArrays()
    {
        // Arrange
        IReadOnlyList<double> x = [];
        IReadOnlyList<double> y = [];

        // Act
        var result = sut.PearsonCorrelation(x, y);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void PearsonCorrelation_ShouldReturnZero_WhenDifferentLengths()
    {
        // Arrange
        IReadOnlyList<double> x = [1.0, 2.0];
        IReadOnlyList<double> y = [1.0, 2.0, 3.0];

        // Act
        var result = sut.PearsonCorrelation(x, y);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void PearsonCorrelation_ShouldReturnZero_WhenBothSeriesConstant()
    {
        // Arrange
        IReadOnlyList<double> x = [5.0, 5.0, 5.0];
        IReadOnlyList<double> y = [5.0, 5.0, 5.0];

        // Act
        var result = sut.PearsonCorrelation(x, y);

        // Assert
        result.Should().Be(0);
    }

    // --- SumSeries edge cases ---

    [Fact]
    public void SumSeries_ShouldReturnOriginalSeries_WhenSingleSeriesProvided()
    {
        // Arrange
        IReadOnlyList<IReadOnlyList<double>> series =
        [
            new double[] { 10.0, 20.0, 30.0 }
        ];

        // Act
        var result = sut.SumSeries(series);

        // Assert
        result.Should().BeEquivalentTo([10.0, 20.0, 30.0]);
    }

    // --- OverloadFraction edge cases ---

    [Fact]
    public void OverloadFraction_ShouldReturnZero_WhenEmptyValues()
    {
        // Arrange
        IReadOnlyList<double> values = [];

        // Act
        var result = sut.OverloadFraction(values, 100.0);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void OverloadFraction_ShouldReturnZero_WhenThresholdIsZero()
    {
        // Arrange
        IReadOnlyList<double> values = [10.0, 20.0];

        // Act
        var result = sut.OverloadFraction(values, 0.0);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void OverloadFraction_ShouldReturnZero_WhenThresholdIsNegative()
    {
        // Arrange
        IReadOnlyList<double> values = [10.0, 20.0];

        // Act
        var result = sut.OverloadFraction(values, -5.0);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void OverloadFraction_ShouldNotCountEqual_WhenValueEqualsThreshold()
    {
        // Arrange -- value at threshold is NOT counted (strictly greater than)
        IReadOnlyList<double> values = [50.0];

        // Act
        var result = sut.OverloadFraction(values, 50.0);

        // Assert
        result.Should().Be(0);
    }
}
