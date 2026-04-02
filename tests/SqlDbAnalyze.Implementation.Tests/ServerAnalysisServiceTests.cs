using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class ServerAnalysisServiceTests
{
    private readonly IAzureMetricsService azureMetricsServiceMock = Substitute.For<IAzureMetricsService>();
    private readonly IDtuAnalysisService dtuAnalysisServiceMock = Substitute.For<IDtuAnalysisService>();
    private readonly ServerAnalysisService sut;

    private const string SubscriptionId = "sub-123";
    private const string ResourceGroupName = "rg-test";
    private const string ServerName = "sql-server-01";
    private readonly TimeSpan timeRange = TimeSpan.FromHours(24);

    public ServerAnalysisServiceTests()
    {
        sut = new ServerAnalysisService(azureMetricsServiceMock, dtuAnalysisServiceMock);
    }

    [Fact]
    public async Task AnalyzeServerAsync_ShouldReturnRecommendation_WhenDatabasesExist()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var dbNames = new List<string> { "db1", "db2" };
        var baseTime = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

        var db1Metrics = new List<DtuMetric>
        {
            new("db1", baseTime, 50.0),
            new("db1", baseTime.AddMinutes(5), 60.0),
        };
        var db2Metrics = new List<DtuMetric>
        {
            new("db2", baseTime, 30.0),
        };

        var db1Summary = new DatabaseDtuSummary("db1", 55.0, 60.0, 100);
        var db2Summary = new DatabaseDtuSummary("db2", 30.0, 30.0, 50);
        var expectedRecommendation = new ElasticPoolRecommendation(
            "Standard", 100, 70.0, [db1Summary, db2Summary]);

        azureMetricsServiceMock.GetDatabaseNamesAsync(
                SubscriptionId, ResourceGroupName, ServerName, cancellationToken)
            .Returns(dbNames);

        azureMetricsServiceMock.GetDtuMetricsAsync(
                SubscriptionId, ResourceGroupName, ServerName, "db1", timeRange, cancellationToken)
            .Returns(db1Metrics);
        azureMetricsServiceMock.GetDtuMetricsAsync(
                SubscriptionId, ResourceGroupName, ServerName, "db2", timeRange, cancellationToken)
            .Returns(db2Metrics);

        azureMetricsServiceMock.GetDatabaseDtuLimitAsync(
                SubscriptionId, ResourceGroupName, ServerName, "db1", cancellationToken)
            .Returns(100);
        azureMetricsServiceMock.GetDatabaseDtuLimitAsync(
                SubscriptionId, ResourceGroupName, ServerName, "db2", cancellationToken)
            .Returns(50);

        dtuAnalysisServiceMock.Summarize("db1", db1Metrics, 100).Returns(db1Summary);
        dtuAnalysisServiceMock.Summarize("db2", db2Metrics, 50).Returns(db2Summary);

        dtuAnalysisServiceMock.Recommend(
                Arg.Is<IReadOnlyList<DatabaseDtuSummary>>(s =>
                    s.Count == 2 && s[0] == db1Summary && s[1] == db2Summary))
            .Returns(expectedRecommendation);

        // Act
        var result = await sut.AnalyzeServerAsync(
            SubscriptionId, ResourceGroupName, ServerName, timeRange, null, cancellationToken);

        // Assert
        result.Should().Be(expectedRecommendation);

        await azureMetricsServiceMock.Received(1).GetDatabaseNamesAsync(
            SubscriptionId, ResourceGroupName, ServerName, cancellationToken);
        await azureMetricsServiceMock.Received(1).GetDtuMetricsAsync(
            SubscriptionId, ResourceGroupName, ServerName, "db1", timeRange, cancellationToken);
        await azureMetricsServiceMock.Received(1).GetDtuMetricsAsync(
            SubscriptionId, ResourceGroupName, ServerName, "db2", timeRange, cancellationToken);
        await azureMetricsServiceMock.Received(1).GetDatabaseDtuLimitAsync(
            SubscriptionId, ResourceGroupName, ServerName, "db1", cancellationToken);
        await azureMetricsServiceMock.Received(1).GetDatabaseDtuLimitAsync(
            SubscriptionId, ResourceGroupName, ServerName, "db2", cancellationToken);
    }

    [Fact]
    public async Task AnalyzeServerAsync_ShouldReturnEmptyRecommendation_WhenNoDatabasesFound()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        IReadOnlyList<string> emptyDbNames = [];
        var expectedRecommendation = new ElasticPoolRecommendation("Basic", 50, 0.0, []);

        azureMetricsServiceMock.GetDatabaseNamesAsync(
                SubscriptionId, ResourceGroupName, ServerName, cancellationToken)
            .Returns(emptyDbNames);

        dtuAnalysisServiceMock.Recommend(
                Arg.Is<IReadOnlyList<DatabaseDtuSummary>>(s => s.Count == 0))
            .Returns(expectedRecommendation);

        // Act
        var result = await sut.AnalyzeServerAsync(
            SubscriptionId, ResourceGroupName, ServerName, timeRange, null, cancellationToken);

        // Assert
        result.Should().Be(expectedRecommendation);

        await azureMetricsServiceMock.Received(1).GetDatabaseNamesAsync(
            SubscriptionId, ResourceGroupName, ServerName, cancellationToken);
        await azureMetricsServiceMock.DidNotReceive().GetDtuMetricsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
        await azureMetricsServiceMock.DidNotReceive().GetDatabaseDtuLimitAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeServerAsync_ShouldPassCancellationToken_WhenTokenProvided()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var dbNames = new List<string> { "db1" };
        var baseTime = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        var metrics = new List<DtuMetric> { new("db1", baseTime, 40.0) };
        var summary = new DatabaseDtuSummary("db1", 40.0, 40.0, 100);
        var recommendation = new ElasticPoolRecommendation("Basic", 50, 40.0, [summary]);

        azureMetricsServiceMock.GetDatabaseNamesAsync(
                SubscriptionId, ResourceGroupName, ServerName, token)
            .Returns(dbNames);
        azureMetricsServiceMock.GetDtuMetricsAsync(
                SubscriptionId, ResourceGroupName, ServerName, "db1", timeRange, token)
            .Returns(metrics);
        azureMetricsServiceMock.GetDatabaseDtuLimitAsync(
                SubscriptionId, ResourceGroupName, ServerName, "db1", token)
            .Returns(100);

        dtuAnalysisServiceMock.Summarize("db1", metrics, 100).Returns(summary);
        dtuAnalysisServiceMock.Recommend(Arg.Any<IReadOnlyList<DatabaseDtuSummary>>())
            .Returns(recommendation);

        // Act
        var result = await sut.AnalyzeServerAsync(
            SubscriptionId, ResourceGroupName, ServerName, timeRange, null, token);

        // Assert
        result.Should().NotBeNull();

        await azureMetricsServiceMock.Received(1).GetDatabaseNamesAsync(
            SubscriptionId, ResourceGroupName, ServerName, token);
        await azureMetricsServiceMock.Received(1).GetDtuMetricsAsync(
            SubscriptionId, ResourceGroupName, ServerName, "db1", timeRange, token);
        await azureMetricsServiceMock.Received(1).GetDatabaseDtuLimitAsync(
            SubscriptionId, ResourceGroupName, ServerName, "db1", token);
    }

    [Fact]
    public async Task AnalyzeServerAsync_ShouldProcessSingleDatabase_WhenOnlyOneDatabaseExists()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var dbNames = new List<string> { "only-db" };
        var baseTime = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        var metrics = new List<DtuMetric> { new("only-db", baseTime, 25.0) };
        var summary = new DatabaseDtuSummary("only-db", 25.0, 25.0, 50);
        var recommendation = new ElasticPoolRecommendation("Basic", 50, 12.5, [summary]);

        azureMetricsServiceMock.GetDatabaseNamesAsync(
                SubscriptionId, ResourceGroupName, ServerName, cancellationToken)
            .Returns(dbNames);
        azureMetricsServiceMock.GetDtuMetricsAsync(
                SubscriptionId, ResourceGroupName, ServerName, "only-db", timeRange, cancellationToken)
            .Returns(metrics);
        azureMetricsServiceMock.GetDatabaseDtuLimitAsync(
                SubscriptionId, ResourceGroupName, ServerName, "only-db", cancellationToken)
            .Returns(50);

        dtuAnalysisServiceMock.Summarize("only-db", metrics, 50).Returns(summary);
        dtuAnalysisServiceMock.Recommend(
                Arg.Is<IReadOnlyList<DatabaseDtuSummary>>(s => s.Count == 1 && s[0] == summary))
            .Returns(recommendation);

        // Act
        var result = await sut.AnalyzeServerAsync(
            SubscriptionId, ResourceGroupName, ServerName, timeRange, null, cancellationToken);

        // Assert
        result.Should().Be(recommendation);
        result.DatabaseSummaries.Should().ContainSingle();
    }

    [Fact]
    public async Task AnalyzeServerAsync_ShouldFilterMetrics_WhenTimeWindowProvided()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var dbNames = new List<string> { "db1" };
        var baseTime = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        var allMetrics = new List<DtuMetric>
        {
            new("db1", baseTime, 50.0),
            new("db1", baseTime.AddHours(12), 70.0),
        };
        var filteredMetrics = new List<DtuMetric>
        {
            new("db1", baseTime, 50.0),
        };
        var timeWindow = new AnalysisTimeWindow(
            new TimeOnly(9, 0), new TimeOnly(17, 0), "Eastern Standard Time");

        var summary = new DatabaseDtuSummary("db1", 50.0, 50.0, 100);
        var recommendation = new ElasticPoolRecommendation("Basic", 50, 50.0, [summary]);

        azureMetricsServiceMock.GetDatabaseNamesAsync(
                SubscriptionId, ResourceGroupName, ServerName, cancellationToken)
            .Returns(dbNames);
        azureMetricsServiceMock.GetDtuMetricsAsync(
                SubscriptionId, ResourceGroupName, ServerName, "db1", timeRange, cancellationToken)
            .Returns(allMetrics);
        azureMetricsServiceMock.GetDatabaseDtuLimitAsync(
                SubscriptionId, ResourceGroupName, ServerName, "db1", cancellationToken)
            .Returns(100);

        dtuAnalysisServiceMock.FilterByTimeWindow(allMetrics, timeWindow)
            .Returns(filteredMetrics);
        dtuAnalysisServiceMock.Summarize("db1", filteredMetrics, 100)
            .Returns(summary);
        dtuAnalysisServiceMock.Recommend(
                Arg.Is<IReadOnlyList<DatabaseDtuSummary>>(s => s.Count == 1 && s[0] == summary))
            .Returns(recommendation);

        // Act
        var result = await sut.AnalyzeServerAsync(
            SubscriptionId, ResourceGroupName, ServerName, timeRange, timeWindow, cancellationToken);

        // Assert
        result.Should().Be(recommendation);
        dtuAnalysisServiceMock.Received(1).FilterByTimeWindow(allMetrics, timeWindow);
    }

    [Fact]
    public async Task AnalyzeServerAsync_ShouldPropagateException_WhenGetDtuMetricsAsyncThrows()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var dbNames = new List<string> { "db1" };
        var expectedException = new InvalidOperationException("Metrics fetch failed");

        azureMetricsServiceMock.GetDatabaseNamesAsync(
                SubscriptionId, ResourceGroupName, ServerName, cancellationToken)
            .Returns(dbNames);

        azureMetricsServiceMock.GetDtuMetricsAsync(
                SubscriptionId, ResourceGroupName, ServerName, "db1", timeRange, cancellationToken)
            .Throws(expectedException);

        // Act
        var act = () => sut.AnalyzeServerAsync(
            SubscriptionId, ResourceGroupName, ServerName, timeRange, null, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.WithMessage("Metrics fetch failed");
    }

    [Fact]
    public async Task AnalyzeServerAsync_ShouldPropagateException_WhenGetDatabaseDtuLimitAsyncThrows()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var dbNames = new List<string> { "db1" };
        var baseTime = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        var metrics = new List<DtuMetric> { new("db1", baseTime, 40.0) };
        var expectedException = new InvalidOperationException("DTU limit fetch failed");

        azureMetricsServiceMock.GetDatabaseNamesAsync(
                SubscriptionId, ResourceGroupName, ServerName, cancellationToken)
            .Returns(dbNames);

        azureMetricsServiceMock.GetDtuMetricsAsync(
                SubscriptionId, ResourceGroupName, ServerName, "db1", timeRange, cancellationToken)
            .Returns(metrics);

        azureMetricsServiceMock.GetDatabaseDtuLimitAsync(
                SubscriptionId, ResourceGroupName, ServerName, "db1", cancellationToken)
            .Throws(expectedException);

        // Act
        var act = () => sut.AnalyzeServerAsync(
            SubscriptionId, ResourceGroupName, ServerName, timeRange, null, cancellationToken);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.WithMessage("DTU limit fetch failed");
    }
}
