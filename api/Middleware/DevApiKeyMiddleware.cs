using System.Net;

namespace api.Middleware;

public class DevApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _expectedKey;
    private static readonly HashSet<string> _allowList = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/swagger",
        "/swagger/index.html"
    };

    public DevApiKeyMiddleware(RequestDelegate next, IConfiguration cfg, IWebHostEnvironment env)
    {
        _next = next;
        if (env.IsDevelopment())
            _expectedKey = cfg["Api:DevApiKey"];
    }

    public async Task Invoke(HttpContext ctx)
    {
        if (_expectedKey is null)
        {
            await _next(ctx);
            return;
        }

        var path = ctx.Request.Path.Value ?? "";
        if (_allowList.Any(a => path.StartsWith(a, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(ctx);
            return;
        }

        if (!ctx.Request.Headers.TryGetValue("X-Api-Key", out var provided) ||
            string.IsNullOrWhiteSpace(provided) ||
            !string.Equals(provided, _expectedKey, StringComparison.Ordinal))
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await ctx.Response.WriteAsJsonAsync(new { error = "Missing or invalid X-Api-Key" });
            return;
        }

        await _next(ctx);
    }
}
