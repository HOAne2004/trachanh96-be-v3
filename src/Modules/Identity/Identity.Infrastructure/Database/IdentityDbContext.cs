using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Database;

public class IdentityDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Address> Addresses { get; set; }

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}