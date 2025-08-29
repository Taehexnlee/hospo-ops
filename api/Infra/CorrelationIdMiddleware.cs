using System.Net;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace api.Infra;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext ctx)
    {
        string? cid = null;

        if (ctx.Request.Headers.TryGetValue(HeaderName, out var vals))
        {
            var incoming = vals.ToString();
            if (Guid.TryParse(incoming, out var g))
                cid = g.ToString();
        }
        cid ??= Guid.NewGuid().ToString();

        // 요청 범위 저장
        ctx.Items[HeaderName] = cid;

        // 응답 시작 직전에 반드시 헤더 삽입
        ctx.Response.OnStarting(() =>
        {
            if (!ctx.Response.Headers.ContainsKey(HeaderName))
                ctx.Response.Headers[HeaderName] = cid!;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = cid! }))
        {
            await _next(ctx);

            // 파이프라인 어딘가가 헤더를 제거/덮었을 때를 대비한 2차 보장
            if (!ctx.Response.Headers.ContainsKey(HeaderName) &&
                ctx.Items.TryGetValue(HeaderName, out var v) &&
                v is string s && !string.IsNullOrWhiteSpace(s))
            {
                ctx.Response.Headers[HeaderName] = s;
            }
        }
    }
}
