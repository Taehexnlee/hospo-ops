using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.Models;

namespace api.Controllers;

[ApiController]
[Route("api/eod")]
public class EodController(AppDbContext db) : ControllerBase
{
    // GET /api/eod?storeId=1&from=2025-08-01&to=2025-08-31&page=1&pageSize=50
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? storeId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page <= 0 || pageSize <= 0) return BadRequest("page/pageSize must be positive.");

        var q = db.EodReports.AsQueryable();

        if (storeId is not null)
            q = q.Where(e => e.StoreId == storeId);

        if (!string.IsNullOrWhiteSpace(from))
        {
            if (!DateOnly.TryParse(from, out var dFrom)) return BadRequest("from must be yyyy-MM-dd");
            q = q.Where(e => e.BizDate >= dFrom);
        }

        if (!string.IsNullOrWhiteSpace(to))
        {
            if (!DateOnly.TryParse(to, out var dTo)) return BadRequest("to must be yyyy-MM-dd");
            q = q.Where(e => e.BizDate <= dTo);
        }

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(e => e.BizDate).ThenBy(e => e.StoreId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    // GET /api/eod/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var eod = await db.EodReports.FindAsync(id);
        return eod is null ? NotFound() : Ok(eod);
    }

    // GET /api/eod/{storeId}/{bizDate}
    [HttpGet("{storeId:int}/{bizDate}")]
    public async Task<IActionResult> Get(int storeId, string bizDate)
    {
        if (!DateOnly.TryParse(bizDate, out var d)) return BadRequest("invalid date");

        var eod = await db.EodReports
            .FirstOrDefaultAsync(x => x.StoreId == storeId && x.BizDate == d);

        return eod is null ? Ok(new { message = "no eod yet" }) : Ok(eod);
    }

    // POST /api/eod
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EodReportDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        // 매장 존재 검사
        var storeExists = await db.Stores.AnyAsync(s => s.Id == dto.StoreId);
        if (!storeExists)
        {
            ModelState.AddModelError("StoreId", "Store not found.");
            return ValidationProblem(ModelState);
        }

        // 날짜 형식 검사
        if (!DateOnly.TryParse(dto.BizDate, out var d))
        {
            ModelState.AddModelError("BizDate", "BizDate must be in yyyy-MM-dd format (yyyy-MM-dd).");
            return ValidationProblem(ModelState);
        }

        // 유니크 제약 (StoreId+BizDate)
        var exists = await db.EodReports.AnyAsync(e => e.StoreId == dto.StoreId && e.BizDate == d);
        if (exists) return Conflict(new { message = "EOD already exists for this store/date." });

        var eod = new EodReport
        {
            StoreId = dto.StoreId,
            BizDate = d,
            NetSales = dto.NetSales,
            Tickets = dto.Tickets,
            CreatedAt = DateTime.UtcNow
        };

        db.EodReports.Add(eod);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = eod.Id }, eod);
    }

    // PUT /api/eod/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EodReportDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        // 매장 존재 검사
        var storeExists = await db.Stores.AnyAsync(s => s.Id == dto.StoreId);
        if (!storeExists)
        {
            ModelState.AddModelError("StoreId", "Store not found.");
            return ValidationProblem(ModelState);
        }

        if (!DateOnly.TryParse(dto.BizDate, out var d))
        {
            ModelState.AddModelError("BizDate", "BizDate must be in yyyy-MM-dd format (yyyy-MM-dd).");
            return ValidationProblem(ModelState);
        }

        var eod = await db.EodReports.FindAsync(id);
        if (eod is null) return NotFound();

        // (StoreId, BizDate) 중복 검사
        var duplicate = await db.EodReports
            .AnyAsync(e => e.Id != id && e.StoreId == dto.StoreId && e.BizDate == d);
        if (duplicate) return Conflict(new { message = "Another EOD already exists for this store/date." });

        eod.StoreId = dto.StoreId;
        eod.BizDate = d;
        eod.NetSales = dto.NetSales;
        eod.Tickets = dto.Tickets;

        await db.SaveChangesAsync();
        return Ok(eod);
    }

    // DELETE /api/eod/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var eod = await db.EodReports.FindAsync(id);
        if (eod is null) return NotFound();

        db.EodReports.Remove(eod);
        await db.SaveChangesAsync();
        return NoContent();
    }
}