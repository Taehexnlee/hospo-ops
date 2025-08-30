using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog.Events;
using api.Infra;
using NetEscapades.AspNetCore.SecurityHeaders;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using api.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;
using api.Data;
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection("Api"));

// Serilog (optional)
builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.MinimumLevel.Information();
    cfg.Enrich.FromLogContext();
    cfg.WriteTo.Console();
});

var cs = builder.Configuration.GetConnectionString("Default")
         ?? "Data Source=hospoops.dev.db";

// Dev => SQLite, Prod => SQL Server
if (builder.Environment.IsDevelopment() ||
    cs.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(cs));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(cs));
}

// MVC
builder.Services.AddControllers().AddJsonOptions(o => { o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; });

// === OpenTelemetry (Tracing + Metrics) ===
builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService(serviceName: "hospo-ops", serviceVersion: "1.0.0"))
    .WithTracing(b =>
    {
        b.AddAspNetCoreInstrumentation(o => {
    o.RecordException = true;
    o.Filter = ctx =>
        !ctx.Request.Path.StartsWithSegments("/health") &&
        !ctx.Request.Path.StartsWithSegments("/swagger");
});
        b.AddHttpClientInstrumentation();
        if (builder.Environment.IsDevelopment())
        {
            b.AddConsoleExporter();
        }
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation();
        m.AddHttpClientInstrumentation();
        if (builder.Environment.IsDevelopment()) {
            m.AddConsoleExporter();
        } else {
            m.AddOtlpExporter();
        }
    });
// === /OpenTelemetry ===

builder.Services.AddCors(o => o.AddPolicy("dev", p => p
    .WithOrigins("http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()
));

// ✅ FluentValidation v11 권장 등록 방식
builder.Services.AddFluentValidationAutoValidation();           // ModelState 자동 검증
builder.Services.AddValidatorsFromAssemblyContaining<Program>(); // 어셈블리 스캔

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "HospoOps API", Version = "v1" });
  c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme {
    Description = "Provide your X-Api-Key",
    In = ParameterLocation.Header,
    Name = "X-Api-Key",
    Type = SecuritySchemeType.ApiKey
  });
  c.AddSecurityRequirement(new OpenApiSecurityRequirement {
    { new OpenApiSecurityScheme {
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
      }, new string[] {} }
  });
});

builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api", httpContext =>
      RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
        factory: key => new FixedWindowRateLimiterOptions {
          PermitLimit = 5,
          Window = TimeSpan.FromSeconds(10),
          AutoReplenishment = true,
          QueueLimit = 0
        }
      )
    );
  });

var app = builder.Build();



app.UseMiddleware<api.Infra.CorrelationIdMiddleware>();
app.UseMiddleware<api.Infra.ApiKeyAuthMiddleware>();
app.UseSecurityHeaders();
app.UseCors("default");
app.UseSerilogRequestLogging();
app.UseMiddleware<api.Infra.GlobalExceptionMiddleware>();
app.UseCors("dev");
app.UseMiddleware<DevApiKeyMiddleware>();
app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { ok = true, ts = DateTime.UtcNow }));

app.Run();
public partial class Program { }
