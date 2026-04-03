using Microsoft.AspNetCore.Mvc;

namespace SqlDbAnalyze.Web.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { Status = "Healthy" });
    }
}
