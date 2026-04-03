using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Web.Core.Controllers;

[ApiController]
[Route("api/analysis")]
public class AnalysisController(
    IMetricsCacheService metricsCacheService) : ControllerBase
{
    [HttpGet("{serverId}/databases")]
    [ProducesResponseType(typeof(IReadOnlyList<DatabaseInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DatabaseInfo>>> GetDatabases(
        int serverId, CancellationToken cancellationToken)
    {
        var databases = await metricsCacheService.GetDatabaseInfoAsync(serverId, cancellationToken);
        return Ok(databases);
    }

    [HttpGet("{serverId}/intervals")]
    [ProducesResponseType(typeof(IReadOnlyList<DatabaseMetricsInterval>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DatabaseMetricsInterval>>> GetCachedIntervals(
        int serverId, CancellationToken cancellationToken)
    {
        var intervals = await metricsCacheService.GetCachedIntervalsAsync(serverId, cancellationToken);
        return Ok(intervals);
    }

    [HttpPost("{serverId}/refresh")]
    [ProducesResponseType(typeof(DtuTimeSeries), StatusCodes.Status200OK)]
    public async Task<ActionResult<DtuTimeSeries>> RefreshMetrics(
        int serverId,
        [FromQuery] int hours = 168,
        CancellationToken cancellationToken = default)
    {
        var timeSeries = await metricsCacheService.RefreshMetricsAsync(
            serverId, hours, cancellationToken);
        return Ok(timeSeries);
    }

    [HttpGet("{serverId}/time-series")]
    [ProducesResponseType(typeof(DtuTimeSeries), StatusCodes.Status200OK)]
    public async Task<ActionResult<DtuTimeSeries>> GetTimeSeries(
        int serverId, CancellationToken cancellationToken)
    {
        var timeSeries = await metricsCacheService.GetCachedTimeSeriesAsync(
            serverId, cancellationToken);
        return Ok(timeSeries);
    }

    [HttpGet("{serverId}/correlation-matrix")]
    [ProducesResponseType(typeof(IReadOnlyList<PoolabilityMetrics>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PoolabilityMetrics>>> GetCorrelationMatrix(
        int serverId, CancellationToken cancellationToken)
    {
        var matrix = await metricsCacheService.GetCorrelationMatrixAsync(
            serverId, cancellationToken);
        return Ok(matrix);
    }

    [HttpPost("{serverId}/simulate-pool")]
    [ProducesResponseType(typeof(PoolSimulationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PoolSimulationResult>> SimulatePool(
        int serverId,
        [FromBody] PoolSimulationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await metricsCacheService.SimulatePoolAsync(
            serverId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{serverId}/build-pools")]
    [ProducesResponseType(typeof(PoolOptimizationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PoolOptimizationResult>> BuildPools(
        int serverId,
        [FromBody] BuildPoolsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await metricsCacheService.BuildPoolsAsync(
            serverId, request, cancellationToken);
        return Ok(result);
    }
}
