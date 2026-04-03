using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Web.Core.Controllers;
using Xunit;

namespace SqlDbAnalyze.Web.Core.Tests.Controllers;

public class AnalysisControllerTests
{
    private readonly IMetricsCacheService _service = Substitute.For<IMetricsCacheService>();

    [Fact]
    public async Task GetDatabases_Should_ReturnOkWithDatabaseInfo_When_ServerExists()
    {
        // Arrange
        var databases = new List<DatabaseInfo>
        {
            new("DbA", 1024.0, 50, null),
            new("DbB", 2048.0, 100, "pool-1")
        };
        _service.GetDatabaseInfoAsync(1, Arg.Any<CancellationToken>()).Returns(databases);
        var controller = new AnalysisController(_service);

        // Act
        var result = await controller.GetDatabases(1, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IReadOnlyList<DatabaseInfo>>().Subject;
        data.Should().HaveCount(2);
        data[0].DatabaseName.Should().Be("DbA");
        data[1].ElasticPoolName.Should().Be("pool-1");
    }

    [Fact]
    public async Task GetCachedIntervals_Should_ReturnOkWithIntervals_When_Called()
    {
        // Arrange
        var intervals = new List<DatabaseMetricsInterval>
        {
            new("DbA", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow, 100)
        };
        _service.GetCachedIntervalsAsync(1, Arg.Any<CancellationToken>()).Returns(intervals);
        var controller = new AnalysisController(_service);

        // Act
        var result = await controller.GetCachedIntervals(1, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IReadOnlyList<DatabaseMetricsInterval>>().Subject;
        data.Should().HaveCount(1);
    }

    [Fact]
    public async Task RefreshMetrics_Should_ReturnOkWithTimeSeries_When_Called()
    {
        // Arrange
        var timeSeries = new DtuTimeSeries([], new Dictionary<string, IReadOnlyList<double>>());
        _service.RefreshMetricsAsync(1, 168, Arg.Any<CancellationToken>()).Returns(timeSeries);
        var controller = new AnalysisController(_service);

        // Act
        var result = await controller.RefreshMetrics(1, 168, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(timeSeries);
    }

    [Fact]
    public async Task GetTimeSeries_Should_ReturnOkWithTimeSeries_When_Called()
    {
        // Arrange
        var timeSeries = new DtuTimeSeries([], new Dictionary<string, IReadOnlyList<double>>());
        _service.GetCachedTimeSeriesAsync(1, Arg.Any<CancellationToken>()).Returns(timeSeries);
        var controller = new AnalysisController(_service);

        // Act
        var result = await controller.GetTimeSeries(1, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(timeSeries);
    }

    [Fact]
    public async Task GetCorrelationMatrix_Should_ReturnOkWithMatrix_When_DataExists()
    {
        // Arrange
        var matrix = new List<PoolabilityMetrics>
        {
            new("DbA", "DbB", 0.5, 0.3, 0.1, 0.2)
        };
        _service.GetCorrelationMatrixAsync(1, Arg.Any<CancellationToken>()).Returns(matrix);
        var controller = new AnalysisController(_service);

        // Act
        var result = await controller.GetCorrelationMatrix(1, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IReadOnlyList<PoolabilityMetrics>>().Subject;
        data.Should().HaveCount(1);
        data[0].PearsonCorrelation.Should().Be(0.5);
    }
}
