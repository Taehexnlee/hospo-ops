using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.Models;

namespace api.Controllers;

[ApiController]
[Route("api/stores")]
// [EnableRateLimiting("api")]
public class StoresController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await db.Stores.OrderBy(s => s.Id).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var store = await db.Stores.FindAsync(id);
        return store is null ? NotFound() : Ok(store);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StoreDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var dup = await db.Stores.AnyAsync(s => s.Name == dto.Name);
        if (dup) return Conflict(new { message = "Store name already exists." });

        var store = new Store { Name = dto.Name };
        db.Stores.Add(store);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = store.Id }, store);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] StoreDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var store = await db.Stores.FindAsync(id);
        if (store is null) return NotFound();

        var dup = await db.Stores.AnyAsync(s => s.Id != id && s.Name == dto.Name);
        if (dup) return Conflict(new { message = "Store name already exists." });

        store.Name = dto.Name;
        await db.SaveChangesAsync();
        return Ok(store);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var store = await db.Stores.FindAsync(id);
        if (store is null) return NotFound();

        db.Stores.Remove(store);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
