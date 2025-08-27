using Microsoft.EntityFrameworkCore;
using api.Models;

namespace api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> opts) : DbContext(opts)
{
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<SquareEventRaw> SquareEvents => Set<SquareEventRaw>();
    public DbSet<EodReport> EodReports => Set<EodReport>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Store>().ToTable("Stores");
        b.Entity<SquareEventRaw>().ToTable("SquareEvents");
        b.Entity<EodReport>().ToTable("EodReports");
        b.Entity<EodReport>().Property(x => x.NetSales).HasPrecision(18, 2);

        b.Entity<EodReport>().HasIndex(x => new { x.StoreId, x.BizDate }).IsUnique();
    }
}
