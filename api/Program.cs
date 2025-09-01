using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using api.Data;
using api.Infra;
using api.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// === Serilog ===
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// === Options / ProblemDetails ===
builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection("Api"));
builder.Services.AddProblemDetails(); // RFC7807

// === DbContext ===
var cs = builder.Configuration.GetConnectionString("Default") ?? "Data Source=hospoops.dev.db";
if (builder.Environment.IsDevelopment() ||
    cs.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(cs));
else
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(cs));

// === MVC ===
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// === OpenTelemetry ===
builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService("hospo-ops", serviceVersion: "1.0.0"))
    .WithTracing(b =>
    {
        b.AddAspNetCoreInstrumentation(o =>
        {
            o.RecordException = true;
            o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health")
                            && !ctx.Request.Path.StartsWithSegments("/swagger");
        });
        b.AddHttpClientInstrumentation();
        if (builder.Environment.IsDevelopment()) b.AddConsoleExporter();
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation();
        m.AddHttpClientInstrumentation();
        if (builder.Environment.IsDevelopment()) m.AddConsoleExporter();
        else m.AddOtlpExporter();
    });

// === CORS ===
builder.Services.AddCors(o =>
{
    o.AddPolicy("dev", p => p
        .WithOrigins("http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod());

    var allowed = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    o.AddPolicy("default", p =>
    {
        if (allowed.Length > 0) p.WithOrigins(allowed);
        p.WithHeaders("X-Api-Key", "Content-Type")
         .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
         .WithExposedHeaders("X-Correlation-Id", "X-RateLimit-Limit", "X-RateLimit-Window", "Retry-After");
    });
});

// === FluentValidation ===
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// === Swagger ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HospoOps API", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Provide your X-Api-Key",
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
      { new OpenApiSecurityScheme {
          Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
        }, Array.Empty<string>() }
    });
});

// === Health Checks ===
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

// === RateLimiter ===
// 테스트에서 Employees가 429 맞지 않도록 전역 강제는 하지 않고, StoresController에만 적용합니다.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 정책 이름: "api" (10초에 5회, 대기열 없음)
    options.AddFixedWindowLimiter("api", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromSeconds(10);
        o.AutoReplenishment = true;
        o.QueueLimit = 0;
    });

    // 필요 시 429 응답 헤더
    options.OnRejected = (ctx, _) =>
    {
        ctx.HttpContext.Response.Headers["Retry-After"] = "10";
        return ValueTask.CompletedTask;
    };
});

var app = builder.Build();

// === Pipeline ===
app.UseRouting();

// 운영에서만 HTTPS 강제
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Preflight(OPTIONS): 빠른 204
app.UseWhen(ctx => HttpMethods.IsOptions(ctx.Request.Method), branch =>
{
    branch.UseCors(app.Environment.IsDevelopment() ? "dev" : "default");
    branch.Run(context =>
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return Task.CompletedTask;
    });
});

// 공통 CORS
app.UseCors(app.Environment.IsDevelopment() ? "dev" : "default");

// 상관관계 ID
app.UseMiddleware<CorrelationIdMiddleware>();

// 요청 로깅
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        if (http.Request.Headers.TryGetValue("X-Api-Key", out var _))
            diag.Set("X-Api-Key", "***redacted***");
    };
});

// 간단 보안 헤더
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "object-src 'none'; form-action 'self'; frame-ancestors 'none'";
    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
    context.Response.Headers["Cross-Origin-Embedder-Policy"] = "credentialless";
    context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-site";
    await next();
});

// (중요) RateLimiter는 라우팅 뒤, 인증/권한 앞
app.UseRateLimiter();

// API Key
app.UseMiddleware<ApiKeyAuthMiddleware>();

// 개발 전용
if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<DevApiKeyMiddleware>();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoints
app.MapGet("/health", () => Results.Ok(new { ok = true, ts = DateTime.UtcNow }))
   .DisableRateLimiting(); // 헬스체크는 제외

// 테스트 간섭 방지를 위해 전역 강제 적용은 하지 않습니다.
// => StoresController에 [EnableRateLimiting("api")]로만 적용
app.MapControllers();

app.Run();

public partial class Program { }