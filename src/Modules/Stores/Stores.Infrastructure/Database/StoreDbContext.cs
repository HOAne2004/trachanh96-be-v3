using Microsoft.EntityFrameworkCore;
using Stores.Domain.Entities;

namespace Stores.Infrastructure.Database;

public class StoreDbContext : DbContext
{
    public StoreDbContext(DbContextOptions<StoreDbContext> options) : base(options)
    {
    }

    // Các DbSet đại diện cho các bảng
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StoreOperatingHour> StoreOperatingHours => Set<StoreOperatingHour>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Table> Tables => Set<Table>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tự động quét và áp dụng tất cả các class IEntityTypeConfiguration ở trên
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StoreDbContext).Assembly);
    }
}