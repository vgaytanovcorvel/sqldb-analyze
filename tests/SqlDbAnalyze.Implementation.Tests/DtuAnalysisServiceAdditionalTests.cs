using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class DtuAnalysisServiceAdditionalTests
{
    private readonly DtuAnalysisService sut = new();

    [Fact]
    public void Summarize_ShouldHandleAllSameValues_WhenConstantMetrics()
    {
        // Arrange
        var baseTime = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        IReadOnlyList<DtuMetric> metrics =
        [
            new DtuMetric("db1", baseTime, 50.0),
            new DtuMetric("db1", baseTime.AddMinutes(5), 50.0),
            new DtuMetric("db1", baseTime.AddMinutes(10), 50.0),
        ];

        // Act
        var result = sut.Summarize("db1", metrics, 100);

        // Assert
        result.AverageDtuPercent.Should().Be(50.0);
        result.PeakDtuPercent.Should().Be(50.0);
    }

    [Fact]
    public void Recommend_ShouldAccumulateMultipleDatabases_WhenManyProvided()
    {
        // Arrange -- 10 databases each with 50% of 100 DTU = 500 total DTU
        var summaries = Enumerable.Range(1, 10)
            .Select(i => new DatabaseDtuSummary($"db{i}", 50.0, 80.0, 100))
            .ToList();

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.EstimatedTotalDtuUsage.Should().Be(500.0);
        result.RecommendedTier.Should().Be("Standard");
        result.RecommendedDtu.Should().Be(800);
        result.DatabaseSummaries.Should().HaveCount(10);
    }

    [Fact]
    public void AggregateByHour_ShouldHandleMultipleDatabases_WhenMixedInSameHour()
    {
        // Arrange -- different database names but same hour
        var hour = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        IReadOnlyList<DtuMetric> metrics =
        [
            new DtuMetric("db1", hour.AddMinutes(5), 20.0),
            new DtuMetric("db2", hour.AddMinutes(10), 80.0),
        ];

        // Act
        var result = sut.AggregateByHour(metrics);

        // Assert -- both metrics are in the same hour
        result.Should().HaveCount(1);
        result[0].AverageDtuPercent.Should().Be(50.0);
        result[0].MaxDtuPercent.Should().Be(80.0);
    }

    [Fact]
    public void FilterByTimeWindow_ShouldHandleMidnightStart_WhenWindowStartsAtMidnight()
    {
        // Arrange -- Window 00:00 to 08:00 ET
        // 04:00 UTC = 00:00 ET (exactly at start, included)
        var timestamp = new DateTimeOffset(2026, 3, 15, 4, 0, 0, TimeSpan.Zero);
        IReadOnlyList<DtuMetric> metrics = [new DtuMetric("db1", timestamp, 50.0)];
        var window = new AnalysisTimeWindow(
            new TimeOnly(0, 0), new TimeOnly(8, 0), "Eastern Standard Time");

        // Act
        var result = sut.FilterByTimeWindow(metrics, window);

        // Assert
        result.Should().HaveCount(1);
    }
}
