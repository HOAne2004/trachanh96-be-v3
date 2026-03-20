using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Identity.Infrastructure.Database;

public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        // Xác định environment
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                          ?? "Development";

        // Path tới API project (startup project)
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../BeverageSystem.Api");

        // Load configuration
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName);
            options.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
        });

        return new IdentityDbContext(optionsBuilder.Options);
    }
}