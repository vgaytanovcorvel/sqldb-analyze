using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class DtuAnalysisServiceTests
{
    private readonly DtuAnalysisService sut = new();

    // --- AggregateByHour ---

    [Fact]
    public void AggregateByHour_ShouldReturnEmpty_WhenMetricsListIsEmpty()
    {
        // Arrange
        IReadOnlyList<DtuMetric> metrics = [];

        // Act
        var result = sut.AggregateByHour(metrics);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void AggregateByHour_ShouldReturnSingleAggregate_WhenSingleMetricProvided()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 3, 15, 10, 30, 0, TimeSpan.Zero);
        IReadOnlyList<DtuMetric> metrics = [new DtuMetric("db1", timestamp, 45.0)];

        // Act
        var result = sut.AggregateByHour(metrics);

        // Assert
        result.Should().HaveCount(1);
        var aggregate = result[0];
        aggregate.Hour.Should().Be(new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero));
        aggregate.AverageDtuPercent.Should().Be(45.0);
        aggregate.MaxDtuPercent.Should().Be(45.0);
    }

    [Fact]
    public void AggregateByHour_ShouldGroupMetricsByHour_WhenMultipleMetricsAcrossHours()
    {
        // Arrange
        var hour1Base = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        var hour2Base = new DateTimeOffset(2026, 3, 15, 11, 0, 0, TimeSpan.Zero);

        IReadOnlyList<DtuMetric> metrics =
        [
            new DtuMetric("db1", hour1Base.AddMinutes(5), 20.0),
            new DtuMetric("db1", hour1Base.AddMinutes(10), 40.0),
            new DtuMetric("db1", hour1Base.AddMinutes(15), 60.0),
            new DtuMetric("db1", hour2Base.AddMinutes(5), 80.0),
            new DtuMetric("db1", hour2Base.AddMinutes(10), 100.0),
        ];

        // Act
        var result = sut.AggregateByHour(metrics);

        // Assert
        result.Should().HaveCount(2);

        result[0].Hour.Should().Be(hour1Base);
        result[0].AverageDtuPercent.Should().Be(40.0);
        result[0].MaxDtuPercent.Should().Be(60.0);

        result[1].Hour.Should().Be(hour2Base);
        result[1].AverageDtuPercent.Should().Be(90.0);
        result[1].MaxDtuPercent.Should().Be(100.0);
    }

    [Fact]
    public void AggregateByHour_ShouldOrderByHour_WhenMetricsAreOutOfOrder()
    {
        // Arrange
        var laterHour = new DateTimeOffset(2026, 3, 15, 14, 0, 0, TimeSpan.Zero);
        var earlierHour = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

        IReadOnlyList<DtuMetric> metrics =
        [
            new DtuMetric("db1", laterHour.AddMinutes(5), 50.0),
            new DtuMetric("db1", earlierHour.AddMinutes(5), 30.0),
        ];

        // Act
        var result = sut.AggregateByHour(metrics);

        // Assert
        result.Should().HaveCount(2);
        result[0].Hour.Should().Be(earlierHour);
        result[1].Hour.Should().Be(laterHour);
    }

    [Fact]
    public void AggregateByHour_ShouldPreserveTimezoneOffset_WhenMetricsHaveOffset()
    {
        // Arrange
        var offset = TimeSpan.FromHours(3);
        var timestamp = new DateTimeOffset(2026, 3, 15, 10, 30, 0, offset);

        IReadOnlyList<DtuMetric> metrics = [new DtuMetric("db1", timestamp, 55.0)];

        // Act
        var result = sut.AggregateByHour(metrics);

        // Assert
        result.Should().HaveCount(1);
        result[0].Hour.Should().Be(new DateTimeOffset(2026, 3, 15, 10, 0, 0, offset));
    }

    // --- Summarize ---

    [Fact]
    public void Summarize_ShouldReturnZeroValues_WhenMetricsListIsEmpty()
    {
        // Arrange
        IReadOnlyList<DtuMetric> metrics = [];

        // Act
        var result = sut.Summarize("testdb", metrics, 100);

        // Assert
        result.DatabaseName.Should().Be("testdb");
        result.AverageDtuPercent.Should().Be(0.0);
        result.PeakDtuPercent.Should().Be(0.0);
        result.CurrentDtuLimit.Should().Be(100);
    }

    [Fact]
    public void Summarize_ShouldCalculateAverageAndPeak_WhenMetricsProvided()
    {
        // Arrange
        var baseTime = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        IReadOnlyList<DtuMetric> metrics =
        [
            new DtuMetric("mydb", baseTime, 10.0),
            new DtuMetric("mydb", baseTime.AddMinutes(5), 30.0),
            new DtuMetric("mydb", baseTime.AddMinutes(10), 50.0),
            new DtuMetric("mydb", baseTime.AddMinutes(15), 70.0),
        ];

        // Act
        var result = sut.Summarize("mydb", metrics, 200);

        // Assert
        result.DatabaseName.Should().Be("mydb");
        result.AverageDtuPercent.Should().Be(40.0);
        result.PeakDtuPercent.Should().Be(70.0);
        result.CurrentDtuLimit.Should().Be(200);
    }

    [Fact]
    public void Summarize_ShouldReturnSameValues_WhenSingleMetricProvided()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        IReadOnlyList<DtuMetric> metrics = [new DtuMetric("singledb", timestamp, 75.5)];

        // Act
        var result = sut.Summarize("singledb", metrics, 50);

        // Assert
        result.AverageDtuPercent.Should().Be(75.5);
        result.PeakDtuPercent.Should().Be(75.5);
    }

    // --- Recommend ---

    [Fact]
    public void Recommend_ShouldReturnBasic50_WhenDatabaseListIsEmpty()
    {
        // Arrange
        IReadOnlyList<DatabaseDtuSummary> summaries = [];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Basic");
        result.RecommendedDtu.Should().Be(50);
        result.EstimatedTotalDtuUsage.Should().Be(0.0);
        result.DatabaseSummaries.Should().BeEmpty();
    }

    [Fact]
    public void Recommend_ShouldReturnBasic50_WhenTotalDtuIsUnder50()
    {
        // Arrange -- 50% of 50 DTU limit = 25 estimated DTU
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 50.0, 80.0, 50),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Basic");
        result.RecommendedDtu.Should().Be(50);
        result.EstimatedTotalDtuUsage.Should().Be(25.0);
    }

    [Fact]
    public void Recommend_ShouldReturnBasic50_WhenTotalDtuIsExactly50()
    {
        // Arrange -- 100% of 50 DTU limit = 50.0 exactly (at boundary, <= 50)
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 100.0, 100.0, 50),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Basic");
        result.RecommendedDtu.Should().Be(50);
        result.EstimatedTotalDtuUsage.Should().Be(50.0);
    }

    [Fact]
    public void Recommend_ShouldReturnStandard100_WhenTotalDtuIsJustAbove50()
    {
        // Arrange -- 50.001 DTU: 50.001% of 100 DTU limit = 50.001
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 50.001, 80.0, 100),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Standard");
        result.RecommendedDtu.Should().Be(100);
        result.EstimatedTotalDtuUsage.Should().BeApproximately(50.001, 0.0001);
    }

    [Fact]
    public void Recommend_ShouldReturnStandard100_WhenTotalDtuIsExactly100()
    {
        // Arrange -- 100% of 100 DTU limit = 100.0 exactly (at boundary, <= 100)
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 100.0, 100.0, 100),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Standard");
        result.RecommendedDtu.Should().Be(100);
        result.EstimatedTotalDtuUsage.Should().Be(100.0);
    }

    [Fact]
    public void Recommend_ShouldReturnStandard200_WhenTotalDtuIsJustAbove100()
    {
        // Arrange -- 100.1% of 100 DTU limit = 100.1
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 100.1, 100.1, 100),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Standard");
        result.RecommendedDtu.Should().Be(200);
        result.EstimatedTotalDtuUsage.Should().BeApproximately(100.1, 0.0001);
    }

    [Fact]
    public void Recommend_ShouldReturnStandard200_WhenTotalDtuIsExactly200()
    {
        // Arrange -- 100% of 200 DTU limit = 200.0 exactly (at boundary, <= 200)
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 100.0, 100.0, 200),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Standard");
        result.RecommendedDtu.Should().Be(200);
        result.EstimatedTotalDtuUsage.Should().Be(200.0);
    }

    [Fact]
    public void Recommend_ShouldReturnStandard400_WhenTotalDtuIsJustAbove200()
    {
        // Arrange -- 200.1 DTU: 50.025% of 400 DTU limit = 200.1
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 50.025, 85.0, 400),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Standard");
        result.RecommendedDtu.Should().Be(400);
        result.EstimatedTotalDtuUsage.Should().BeApproximately(200.1, 0.0001);
    }

    [Fact]
    public void Recommend_ShouldReturnStandard400_WhenTotalDtuIsExactly400()
    {
        // Arrange -- 100% of 400 DTU limit = 400.0 exactly (at boundary, <= 400)
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 100.0, 100.0, 400),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Standard");
        result.RecommendedDtu.Should().Be(400);
        result.EstimatedTotalDtuUsage.Should().Be(400.0);
    }

    [Fact]
    public void Recommend_ShouldReturnStandard800_WhenTotalDtuIsJustAbove400()
    {
        // Arrange -- 400.1 DTU: 50.0125% of 800 DTU limit = 400.1
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 50.0125, 80.0, 800),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Standard");
        result.RecommendedDtu.Should().Be(800);
        result.EstimatedTotalDtuUsage.Should().BeApproximately(400.1, 0.0001);
    }

    [Fact]
    public void Recommend_ShouldReturnStandard800_WhenTotalDtuIsExactly800()
    {
        // Arrange -- 100% of 800 DTU limit = 800.0 exactly (at boundary, <= 800)
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 100.0, 100.0, 800),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Standard");
        result.RecommendedDtu.Should().Be(800);
        result.EstimatedTotalDtuUsage.Should().Be(800.0);
    }

    [Fact]
    public void Recommend_ShouldReturnStandard1200_WhenTotalDtuIsJustAbove800()
    {
        // Arrange -- 800.1 DTU: 66.675% of 1200 DTU limit = 800.1
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 66.675, 80.0, 1200),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Standard");
        result.RecommendedDtu.Should().Be(1200);
        result.EstimatedTotalDtuUsage.Should().BeApproximately(800.1, 0.0001);
    }

    [Fact]
    public void Recommend_ShouldReturnStandard1200_WhenTotalDtuIsExactly1200()
    {
        // Arrange -- 100% of 1200 DTU limit = 1200.0 exactly (at boundary, <= 1200)
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 100.0, 100.0, 1200),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Standard");
        result.RecommendedDtu.Should().Be(1200);
        result.EstimatedTotalDtuUsage.Should().Be(1200.0);
    }

    [Fact]
    public void Recommend_ShouldReturnPremium1600_WhenTotalDtuIsJustAbove1200()
    {
        // Arrange -- 1200.1 DTU: 75.00625% of 1600 DTU limit = 1200.1
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 75.00625, 80.0, 1600),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Premium");
        result.RecommendedDtu.Should().Be(1600);
        result.EstimatedTotalDtuUsage.Should().BeApproximately(1200.1, 0.0001);
    }

    [Fact]
    public void Recommend_ShouldReturnPremium1600_WhenTotalDtuIsExactly1600()
    {
        // Arrange -- 100% of 1600 DTU limit = 1600.0 exactly (at boundary, <= 1600)
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 100.0, 100.0, 1600),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Premium");
        result.RecommendedDtu.Should().Be(1600);
        result.EstimatedTotalDtuUsage.Should().Be(1600.0);
    }

    [Fact]
    public void Recommend_ShouldReturnPremium2000_WhenTotalDtuIsJustAbove1600()
    {
        // Arrange -- 1600.1 DTU: 80.005% of 2000 DTU limit = 1600.1
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 80.005, 90.0, 2000),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Premium");
        result.RecommendedDtu.Should().Be(2000);
        result.EstimatedTotalDtuUsage.Should().BeApproximately(1600.1, 0.0001);
    }

    [Fact]
    public void Recommend_ShouldReturnPremium2000_WhenTotalDtuExceeds1600()
    {
        // Arrange -- 50% of 4000 DTU limit = 2000 estimated DTU
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 50.0, 80.0, 4000),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Premium");
        result.RecommendedDtu.Should().Be(2000);
    }

    [Fact]
    public void Recommend_ShouldSumDtuAcrossMultipleDatabases_WhenMultipleSummariesProvided()
    {
        // Arrange -- db1: 50% of 100 = 50, db2: 50% of 100 = 50, total = 100
        IReadOnlyList<DatabaseDtuSummary> summaries =
        [
            new DatabaseDtuSummary("db1", 50.0, 80.0, 100),
            new DatabaseDtuSummary("db2", 50.0, 70.0, 100),
        ];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.RecommendedTier.Should().Be("Standard");
        result.RecommendedDtu.Should().Be(100);
        result.EstimatedTotalDtuUsage.Should().Be(100.0);
        result.DatabaseSummaries.Should().HaveCount(2);
    }

    [Fact]
    public void Recommend_ShouldIncludeAllSummariesInResult_WhenSingleDatabaseProvided()
    {
        // Arrange
        var summary = new DatabaseDtuSummary("lonely-db", 10.0, 20.0, 50);
        IReadOnlyList<DatabaseDtuSummary> summaries = [summary];

        // Act
        var result = sut.Recommend(summaries);

        // Assert
        result.DatabaseSummaries.Should().ContainSingle()
            .Which.Should().Be(summary);
    }
}
