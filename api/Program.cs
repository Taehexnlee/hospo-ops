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

var builder = WebApplication.CreateBuilder(args);

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
          PermitLimit = 30,
          Window = TimeSpan.FromSeconds(10),
          AutoReplenishment = true,
          QueueLimit = 0
        }
      )
    );
  });

var app = builder.Build();


app.UseMiddleware<api.Infra.CorrelationIdMiddleware>();
app.UseSecurityHeaders();
app.UseCors("default");
app.UseSerilogRequestLogging();
app.UseRateLimiter();

app.UseMiddleware<api.Infra.GlobalExceptionMiddleware>();
app.UseCors("dev");
app.UseMiddleware<DevApiKeyMiddleware>();
app.UseSerilogRequestLogging();

app.UseRateLimiter();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { ok = true, ts = DateTime.UtcNow }));

app.Run();
public partial class Program { }
