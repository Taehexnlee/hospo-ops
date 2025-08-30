using System.Net;
using Microsoft.Extensions.Options;

namespace api.Infra;

public sealed class ApiKeyOptions
{
    public string? DevApiKey { get; set; }
}

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;
    private readonly ApiKeyOptions _opts;

    public const string HeaderName = "X-Api-Key";

    private static readonly PathString[] _allowAnonymous =
    {
        "/health", "/swagger", "/swagger/index.html", "/swagger/v1/swagger.json"
    };

    public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger, IOptions<ApiKeyOptions> opts)
    {
        _next = next;
        _logger = logger;
        _opts = opts.Value;
    }

    public async Task Invoke(HttpContext ctx)
    {
        // 익명 허용 경로는 통과
        if (_allowAnonymous.Any(p => ctx.Request.Path.StartsWithSegments(p)))
        {
            await _next(ctx);
            return;
        }

        var configured = _opts.DevApiKey;
        if (string.IsNullOrWhiteSpace(configured))
        {
            _logger.LogWarning("API key not configured; denying all non-anonymous requests.");
            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }

        // 헤더 확인
        if (!ctx.Request.Headers.TryGetValue(HeaderName, out var vals))
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }

        var incoming = vals.ToString();
        if (!string.Equals(incoming, configured, StringComparison.Ordinal))
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return;
        }

        await _next(ctx);
    }
}
