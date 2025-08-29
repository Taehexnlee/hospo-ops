using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.Models;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
[EnableRateLimiting("api")]
    public class EodController : ControllerBase
    {
        private readonly AppDbContext _db;
        public EodController(AppDbContext db) => _db = db;

        // 엔티티 직접 반환 대신 DTO로 반환
        public record EodDto(long Id, int StoreId, DateOnly BizDate, decimal NetSales, DateTime CreatedAt);

        // GET /api/eod?page=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> List(
            int? storeId,
            string? from,
            string? to,
            int page = 1,
            int pageSize = 10)
        {
            var q = _db.EodReports.AsQueryable();

            if (storeId.HasValue) q = q.Where(x => x.StoreId == storeId.Value);
            if (DateOnly.TryParse(from, out var f)) q = q.Where(x => x.BizDate >= f);
            if (DateOnly.TryParse(to, out var t)) q = q.Where(x => x.BizDate <= t);

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(x => x.BizDate).ThenBy(x => x.StoreId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EodDto(x.Id, x.StoreId, x.BizDate, x.NetSales, x.CreatedAt))
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        // GET /api/eod/{id}
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var e = await _db.EodReports
                .Where(x => x.Id == id)
                .Select(x => new EodDto(x.Id, x.StoreId, x.BizDate, x.NetSales, x.CreatedAt))
                .FirstOrDefaultAsync();

            if (e == null) return NotFound();
            return Ok(e);
        }

        // POST /api/eod
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EodReport dto)
        {
            if (dto == null) return BadRequest();
            if (dto.StoreId <= 0 || dto.NetSales < 0) return BadRequest();

            dto.CreatedAt = DateTime.UtcNow;

            _db.EodReports.Add(dto);
            await _db.SaveChangesAsync();

            var created = new EodDto(dto.Id, dto.StoreId, dto.BizDate, dto.NetSales, dto.CreatedAt);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, created);
        }
    }
}
