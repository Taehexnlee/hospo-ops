using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using api.Models;

namespace api.Controllers;

[ApiController]
[Route("api/test-validation")]
[EnableRateLimiting("api")]
public class TestValidationController : ControllerBase
{
    [HttpPost]
    public IActionResult Post(EodReportDto dto)
    {
        // FluentValidation이 자동으로 ModelState 검사
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        return Ok(new { message = "Validation passed", dto });
    }
}
