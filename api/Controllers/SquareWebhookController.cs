using Microsoft.AspNetCore.RateLimiting;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.Models;

namespace api.Controllers;

[ApiController]
[Route("square/webhook")]
[EnableRateLimiting("api")]
public class SquareWebhookController(AppDbContext db, IConfiguration cfg, ILogger<SquareWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        var secret = cfg["Square:WebhookSignatureKey"] ?? "";
        var sig = Request.Headers["X-Square-HmacSHA256-Signature"].FirstOrDefault() ?? "";

        string computed = "";
        if (!string.IsNullOrEmpty(secret))
        {
            using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = h.ComputeHash(Encoding.UTF8.GetBytes(body));
            computed = Convert.ToBase64String(hash);
        }

        var valid = sig == computed || string.IsNullOrEmpty(secret);

        var ev = new SquareEventRaw
        {
            StoreId = 1, // TODO: 매장 매핑 로직
            EventType = Request.Headers["X-Square-Event-Type"].FirstOrDefault() ?? "unknown",
            Signature = sig,
            Payload = body,
            ReceivedAt = DateTime.UtcNow,
            Processed = valid
        };
        db.SquareEvents.Add(ev);
        await db.SaveChangesAsync();

        if (!valid)
        {
            logger.LogWarning("Invalid Square signature. header={Sig} computed={Computed}", sig, computed);
            return Unauthorized();
        }

        // TODO: Sales 적재 & EOD 집계 큐잉
        return Ok(new { ok = true });
    }
}
