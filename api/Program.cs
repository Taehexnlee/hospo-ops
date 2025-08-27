using Microsoft.EntityFrameworkCore;
using Serilog;
using api.Data;

var builder = WebApplication.CreateBuilder(args);

// Serilog(선택)
builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.MinimumLevel.Information();
    cfg.Enrich.FromLogContext();
    cfg.WriteTo.Console();
});

var cs = builder.Configuration.GetConnectionString("Default")
         ?? "Data Source=hospoops.dev.db";

if (builder.Environment.IsDevelopment() ||
    cs.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(cs));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(cs));
}

// MVC/Swagger
builder.Services.AddControllers();
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
