using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace api.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // appsettings.json + appsettings.{Environment}.json 읽기
            var cfg = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var cs = cfg.GetConnectionString("Default") ?? "Data Source=hospoops.dev.db";

            var options = new DbContextOptionsBuilder<AppDbContext>();

            // 개발/SQLite 우선. 연결문자열이 "Data Source=" 로 시작하면 SQLite로 간주
            if (env.Equals("Development", StringComparison.OrdinalIgnoreCase) ||
                cs.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(cs);
            }
            else
            {
                options.UseSqlServer(cs);
            }

            return new AppDbContext(options.Options);
        }
    }
}
