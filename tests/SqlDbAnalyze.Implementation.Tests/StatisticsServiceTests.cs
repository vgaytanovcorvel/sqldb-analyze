using FluentAssertions;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class StatisticsServiceTests
{
    private readonly StatisticsService sut = new();

    // --- Mean ---

    [Fact]
    public void Mean_ShouldReturnZero_WhenListIsEmpty()
    {
        // Arrange
        IReadOnlyList<double> values = [];

        // Act
        var result = sut.Mean(values);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Mean_ShouldReturnValue_WhenSingleElement()
    {
        // Arrange
        IReadOnlyList<double> values = [42.0];

        // Act
        var result = sut.Mean(values);

        // Assert
        result.Should().Be(42.0);
    }

    [Fact]
    public void Mean_ShouldReturnAverage_WhenMultipleValues()
    {
        // Arrange
        IReadOnlyList<double> values = [10.0, 20.0, 30.0];

        // Act
        var result = sut.Mean(values);

        // Assert
        result.Should().Be(20.0);
    }

    // --- Percentile ---

    [Fact]
    public void Percentile_ShouldReturnZero_WhenListIsEmpty()
    {
        // Arrange
        IReadOnlyList<double> values = [];

        // Act
        var result = sut.Percentile(values, 0.5);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Percentile_ShouldReturnMin_WhenPercentileIsZero()
    {
        // Arrange
        IReadOnlyList<double> values = [10.0, 20.0, 30.0];

        // Act
        var result = sut.Percentile(values, 0);

        // Assert
        result.Should().Be(10.0);
    }

    [Fact]
    public void Percentile_ShouldReturnMax_WhenPercentileIsOne()
    {
        // Arrange
        IReadOnlyList<double> values = [10.0, 20.0, 30.0];

        // Act
        var result = sut.Percentile(values, 1.0);

        // Assert
        result.Should().Be(30.0);
    }

    [Fact]
    public void Percentile_ShouldReturnMedian_WhenPercentileIsHalf()
    {
        // Arrange
        IReadOnlyList<double> values = [10.0, 20.0, 30.0];

        // Act
        var result = sut.Percentile(values, 0.5);

        // Assert
        result.Should().Be(20.0);
    }

    [Fact]
    public void Percentile_ShouldInterpolate_WhenIndexIsNotInteger()
    {
        // Arrange — 4 values: [10, 20, 30, 40], p=0.75
        // index = 3 * 0.75 = 2.25, so interpolate between 30 and 40
        // result = 30 + (40 - 30) * 0.25 = 32.5
        IReadOnlyList<double> values = [10.0, 20.0, 30.0, 40.0];

        // Act
        var result = sut.Percentile(values, 0.75);

        // Assert
        result.Should().BeApproximately(32.5, 0.001);
    }

    [Fact]
    public void Percentile_ShouldHandleUnsortedInput_WhenValuesAreNotOrdered()
    {
        // Arrange
        IReadOnlyList<double> values = [30.0, 10.0, 20.0];

        // Act
        var result = sut.Percentile(values, 1.0);

        // Assert
        result.Should().Be(30.0);
    }

    // --- PearsonCorrelation ---

    [Fact]
    public void PearsonCorrelation_ShouldReturnZero_WhenFewerThanTwoPoints()
    {
        // Arrange
        IReadOnlyList<double> x = [1.0];
        IReadOnlyList<double> y = [2.0];

        // Act
        var result = sut.PearsonCorrelation(x, y);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void PearsonCorrelation_ShouldReturnOne_WhenPerfectlyCorrelated()
    {
        // Arrange
        IReadOnlyList<double> x = [1.0, 2.0, 3.0, 4.0, 5.0];
        IReadOnlyList<double> y = [2.0, 4.0, 6.0, 8.0, 10.0];

        // Act
        var result = sut.PearsonCorrelation(x, y);

        // Assert
        result.Should().BeApproximately(1.0, 0.0001);
    }

    [Fact]
    public void PearsonCorrelation_ShouldReturnNegativeOne_WhenPerfectlyAntiCorrelated()
    {
        // Arrange
        IReadOnlyList<double> x = [1.0, 2.0, 3.0, 4.0, 5.0];
        IReadOnlyList<double> y = [10.0, 8.0, 6.0, 4.0, 2.0];

        // Act
        var result = sut.PearsonCorrelation(x, y);

        // Assert
        result.Should().BeApproximately(-1.0, 0.0001);
    }

    [Fact]
    public void PearsonCorrelation_ShouldReturnZero_WhenOneSeriesIsConstant()
    {
        // Arrange
        IReadOnlyList<double> x = [1.0, 2.0, 3.0];
        IReadOnlyList<double> y = [5.0, 5.0, 5.0];

        // Act
        var result = sut.PearsonCorrelation(x, y);

        // Assert
        result.Should().Be(0);
    }

    // --- SumSeries ---

    [Fact]
    public void SumSeries_ShouldReturnEmpty_WhenNoSeriesProvided()
    {
        // Arrange
        IReadOnlyList<IReadOnlyList<double>> series = [];

        // Act
        var result = sut.SumSeries(series);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SumSeries_ShouldReturnElementWiseSum_WhenMultipleSeries()
    {
        // Arrange
        IReadOnlyList<IReadOnlyList<double>> series =
        [
            new double[] { 10.0, 20.0, 30.0 },
            new double[] { 1.0, 2.0, 3.0 },
            new double[] { 100.0, 200.0, 300.0 }
        ];

        // Act
        var result = sut.SumSeries(series);

        // Assert
        result.Should().BeEquivalentTo(new[] { 111.0, 222.0, 333.0 });
    }

    // --- OverloadFraction ---

    [Fact]
    public void OverloadFraction_ShouldReturnZero_WhenNoValuesExceedThreshold()
    {
        // Arrange
        IReadOnlyList<double> values = [10.0, 20.0, 30.0];

        // Act
        var result = sut.OverloadFraction(values, 100.0);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void OverloadFraction_ShouldReturnCorrectFraction_WhenSomeValuesExceed()
    {
        // Arrange — 2 of 4 exceed threshold
        IReadOnlyList<double> values = [10.0, 50.0, 60.0, 20.0];

        // Act
        var result = sut.OverloadFraction(values, 40.0);

        // Assert
        result.Should().Be(0.5);
    }

    [Fact]
    public void OverloadFraction_ShouldReturnOne_WhenAllValuesExceed()
    {
        // Arrange
        IReadOnlyList<double> values = [50.0, 60.0, 70.0];

        // Act
        var result = sut.OverloadFraction(values, 10.0);

        // Assert
        result.Should().Be(1.0);
    }
}
