using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.Models;

namespace api.Controllers;

[ApiController]
[Route("api/employees")]
[EnableRateLimiting("api")]
public class EmployeesController(AppDbContext db) : ControllerBase
{
    // GET /api/employees?storeId=1&active=true&page=1&pageSize=50&name=lee
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? storeId,
        [FromQuery] bool? active,
        [FromQuery] string? name,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page <= 0 || pageSize <= 0) return BadRequest("page/pageSize must be positive.");

        var q = db.Employees.AsQueryable();

        if (storeId is not null) q = q.Where(e => e.StoreId == storeId);
        if (active is not null)  q = q.Where(e => e.Active == active);
        if (!string.IsNullOrWhiteSpace(name))
            q = q.Where(e => e.FullName.Contains(name));

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(e => e.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    // GET /api/employees/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var emp = await db.Employees.FindAsync(id);
        return emp is null ? NotFound() : Ok(emp);
    }

    // POST /api/employees
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeeDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (!await db.Stores.AnyAsync(s => s.Id == dto.StoreId))
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>{
                ["StoreId"] = new[] { "Store not found." }
            }));

        DateOnly? hire = null;
        if (!string.IsNullOrWhiteSpace(dto.HireDate))
        {
            if (!DateOnly.TryParse(dto.HireDate, out var d))
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>{
                    ["HireDate"] = new[] { "HireDate must be yyyy-MM-dd." }
                }));
            hire = d;
        }

        var dup = await db.Employees.AnyAsync(e => e.StoreId == dto.StoreId && e.FullName == dto.FullName);
        if (dup) return Conflict(new { message = "Employee already exists in this store." });

        var emp = new Employee
        {
            StoreId = dto.StoreId,
            FullName = dto.FullName,
            Role = dto.Role,
            HireDate = hire,
            Active = dto.Active
        };

        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = emp.Id }, emp);
    }

    // PUT /api/employees/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var emp = await db.Employees.FindAsync(id);
        if (emp is null) return NotFound();

        if (!await db.Stores.AnyAsync(s => s.Id == dto.StoreId))
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>{
                ["StoreId"] = new[] { "Store not found." }
            }));

        DateOnly? hire = null;
        if (!string.IsNullOrWhiteSpace(dto.HireDate))
        {
            if (!DateOnly.TryParse(dto.HireDate, out var d))
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>{
                    ["HireDate"] = new[] { "HireDate must be yyyy-MM-dd." }
                }));
            hire = d;
        }

        var dup = await db.Employees
            .AnyAsync(e => e.Id != id && e.StoreId == dto.StoreId && e.FullName == dto.FullName);
        if (dup) return Conflict(new { message = "Another employee with same name exists in this store." });

        emp.StoreId = dto.StoreId;
        emp.FullName = dto.FullName;
        emp.Role = dto.Role;
        emp.HireDate = hire;
        emp.Active = dto.Active;

        await db.SaveChangesAsync();
        return Ok(emp);
    }

    // DELETE /api/employees/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var emp = await db.Employees.FindAsync(id);
        if (emp is null) return NotFound();

        db.Employees.Remove(emp);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
