using Catalog.Application.Interfaces;
using Catalog.Infrastructure.Data;
using Catalog.Infrastructure.Database;
using Catalog.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Interceptors;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            var auditableInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();

            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                x => x.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName)
            )
            .AddInterceptors(auditableInterceptor); 
        });

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IToppingRepository, ToppingRepository>();
        services.AddScoped<ICatalogUnitOfWork, CatalogUnitOfWork>();
        return services;
    }

    public static async Task SeedCatalogDataAsync(this IServiceProvider serviceProvider)
    {
        // Tạo một scope mới để lấy DbContext (vì DbContext là Scoped Service)
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        // Gọi Seeder
        await DataSeeder.SeedAsync(context);
    }
}