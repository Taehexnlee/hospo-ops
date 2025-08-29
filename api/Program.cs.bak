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
builder.Services.AddControllers();

// ✅ FluentValidation v11 권장 등록 방식
builder.Services.AddFluentValidationAutoValidation();           // ModelState 자동 검증
builder.Services.AddValidatorsFromAssemblyContaining<Program>(); // 어셈블리 스캔

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { ok = true, ts = DateTime.UtcNow }));

app.Run();