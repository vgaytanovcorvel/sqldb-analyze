using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Web.Core.Controllers;

[ApiController]
[Route("api/registered-servers")]
public class RegisteredServersController(
    IRegisteredServerService registeredServerService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RegisteredServer>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RegisteredServer>>> GetAll(
        CancellationToken cancellationToken)
    {
        var servers = await registeredServerService.GetAllServersAsync(cancellationToken);
        return Ok(servers);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RegisteredServer), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisteredServer>> Create(
        [FromBody] CreateRegisteredServerRequest request,
        CancellationToken cancellationToken)
    {
        var server = await registeredServerService.CreateServerAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), server);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await registeredServerService.DeleteServerAsync(id, cancellationToken);
        return NoContent();
    }
}
