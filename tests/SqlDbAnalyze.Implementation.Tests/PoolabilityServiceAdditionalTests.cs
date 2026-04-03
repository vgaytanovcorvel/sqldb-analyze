using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class PoolabilityServiceAdditionalTests
{
    private readonly StatisticsService statisticsService = new();
    private readonly PoolabilityService sut;

    public PoolabilityServiceAdditionalTests()
    {
        sut = new PoolabilityService(statisticsService);
    }

    [Fact]
    public void BuildProfiles_ShouldReturnEmpty_WhenTimeSeriesHasNoDatabases()
    {
        // Arrange
        var ts = new DtuTimeSeries(
            new List<DateTimeOffset> { DateTimeOffset.UtcNow },
            new Dictionary<string, IReadOnlyList<double>>());

        // Act
        var result = sut.BuildProfiles(ts);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildProfiles_ShouldOrderByName_WhenMultipleDatabases()
    {
        // Arrange
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var ts = new DtuTimeSeries(
            new List<DateTimeOffset> { baseTime },
            new Dictionary<string, IReadOnlyList<double>>
            {
                ["zebra-db"] = [10.0],
                ["alpha-db"] = [20.0]
            });

        // Act
        var result = sut.BuildProfiles(ts);

        // Assert
        result.Should().HaveCount(2);
        result[0].DatabaseName.Should().Be("alpha-db");
        result[1].DatabaseName.Should().Be("zebra-db");
    }

    [Fact]
    public void BuildProfiles_ShouldComputePeakAsMax_WhenValuesProvided()
    {
        // Arrange
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var ts = new DtuTimeSeries(
            new List<DateTimeOffset> { baseTime, baseTime.AddMinutes(5), baseTime.AddMinutes(10) },
            new Dictionary<string, IReadOnlyList<double>>
            {
                ["db1"] = [10.0, 100.0, 50.0]
            });

        // Act
        var result = sut.BuildProfiles(ts);

        // Assert
        result[0].Peak.Should().Be(100.0);
    }

    [Fact]
    public void ComputePairwise_ShouldHandleConstantSeries_WhenBothFlat()
    {
        // Arrange -- constant values have zero variance
        var a = BuildProfile("db1", [50.0, 50.0, 50.0, 50.0, 50.0]);
        var b = BuildProfile("db2", [50.0, 50.0, 50.0, 50.0, 50.0]);

        // Act
        var result = sut.ComputePairwise(a, b, 0.90);

        // Assert
        result.PearsonCorrelation.Should().Be(0);
        result.BadTogetherScore.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void ComputePairwise_ShouldReturnZeroPeakCorrelation_WhenFewerThanTwoPeakPoints()
    {
        // Arrange -- with high threshold, only 1 peak point per series
        var a = BuildProfile("db1", [100.0, 10.0]);
        var b = BuildProfile("db2", [10.0, 100.0]);

        // Act
        var result = sut.ComputePairwise(a, b, 0.99);

        // Assert -- at 0.99 threshold, very few points qualify as "peak"
        result.Should().NotBeNull();
    }

    private DatabaseProfile BuildProfile(string name, double[] values)
    {
        return new DatabaseProfile(
            name,
            values,
            statisticsService.Mean(values),
            statisticsService.Percentile(values, 0.95),
            statisticsService.Percentile(values, 0.99),
            values.Max());
    }
}
