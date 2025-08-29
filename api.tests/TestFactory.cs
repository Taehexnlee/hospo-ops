using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using api.Data;
using Microsoft.EntityFrameworkCore;

public class TestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Data Source=:memory:",
                ["Api:DevApiKey"] = "dev-super-secret"
            };
            config.AddInMemoryCollection(dict);
        });

        builder.ConfigureServices(services =>
        {
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            try { db.Database.Migrate(); }
            catch { db.Database.EnsureCreated(); }
        });
    }
}
