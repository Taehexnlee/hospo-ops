using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("health2")]
// [EnableRateLimiting("api")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { ok = true, ts = DateTime.UtcNow });
}
