using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;

namespace api.Controllers;

[ApiController]
[Route("api/eod")]
public class EodController(AppDbContext db) : ControllerBase
{
    [HttpGet("{storeId:int}/{bizDate}")]
    public async Task<IActionResult> Get(int storeId, string bizDate)
    {
        if (!DateOnly.TryParse(bizDate, out var d)) return BadRequest("invalid date");

        var eod = await db.EodReports
            .FirstOrDefaultAsync(x => x.StoreId == storeId && x.BizDate == d);

        return eod is null
            ? Ok(new { message = "no eod yet" })
            : Ok(eod);
    }
}
