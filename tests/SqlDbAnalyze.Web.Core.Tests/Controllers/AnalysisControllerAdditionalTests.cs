using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Web.Core.Controllers;
using Xunit;

namespace SqlDbAnalyze.Web.Core.Tests.Controllers;

public class AnalysisControllerAdditionalTests
{
    private readonly IMetricsCacheService _service = Substitute.For<IMetricsCacheService>();

    [Fact]
    public async Task SimulatePool_Should_ReturnOkWithResult_When_ValidRequest()
    {
        // Arrange
        var request = new PoolSimulationRequest(
            new List<string> { "db1", "db2" },
            new Dictionary<string, int> { ["db1"] = 100, ["db2"] = 200 });
        var simulationResult = new PoolSimulationResult(
            request.DatabaseNames, 80.0, 95.0, 110.0, 50.0, 1.5, 0.001, 104.5, 300.0, 65.0);
        _service.SimulatePoolAsync(1, request, Arg.Any<CancellationToken>()).Returns(simulationResult);
        var controller = new AnalysisController(_service);

        // Act
        var result = await controller.SimulatePool(1, request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<PoolSimulationResult>().Subject;
        data.P99Dtu.Should().Be(95.0);
        data.RecommendedPoolDtu.Should().Be(104.5);
    }

    [Fact]
    public async Task BuildPools_Should_ReturnOkWithResult_When_ValidRequest()
    {
        // Arrange
        var request = new BuildPoolsRequest(
            new List<string> { "db1", "db2" },
            new Dictionary<string, int> { ["db1"] = 100, ["db2"] = 200 });
        var poolResult = new PoolOptimizationResult(
            new List<PoolAssignment>
            {
                new(0, new List<string> { "db1", "db2" }, 150.0, 120.0, 140.0, 160.0, 1.5, 0.0)
            },
            150.0,
            new List<string>());
        _service.BuildPoolsAsync(1, request, Arg.Any<CancellationToken>()).Returns(poolResult);
        var controller = new AnalysisController(_service);

        // Act
        var result = await controller.BuildPools(1, request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<PoolOptimizationResult>().Subject;
        data.Pools.Should().HaveCount(1);
        data.TotalRequiredCapacity.Should().Be(150.0);
    }

    [Fact]
    public async Task GetDatabases_Should_ReturnOkWithEmptyList_When_NoDatabases()
    {
        // Arrange
        _service.GetDatabaseInfoAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<DatabaseInfo>());
        var controller = new AnalysisController(_service);

        // Act
        var result = await controller.GetDatabases(1, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IReadOnlyList<DatabaseInfo>>().Subject;
        data.Should().BeEmpty();
    }

    [Fact]
    public async Task RefreshMetrics_Should_UseDefaultHours_When_NotSpecified()
    {
        // Arrange
        var timeSeries = new DtuTimeSeries([], new Dictionary<string, IReadOnlyList<double>>());
        _service.RefreshMetricsAsync(1, 168, Arg.Any<CancellationToken>()).Returns(timeSeries);
        var controller = new AnalysisController(_service);

        // Act
        var result = await controller.RefreshMetrics(1, cancellationToken: CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        await _service.Received(1).RefreshMetricsAsync(1, 168, Arg.Any<CancellationToken>());
    }
}
