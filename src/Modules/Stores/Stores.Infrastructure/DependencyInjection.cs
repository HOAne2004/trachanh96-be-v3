using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Interceptors;
using Stores.Infrastructure.Database;
using Stores.Application.Interfaces;
using Stores.Infrastructure.Services;

namespace Stores.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddStoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Đăng ký Interceptor chung từ tầng Shared
        services.AddScoped<AuditableEntityInterceptor>();

        // Đăng ký StoreDbContext
        services.AddDbContext<StoreDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            // Add interceptor để tự động cập nhật CreatedAt, UpdatedAt, IsDeleted
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            options.AddInterceptors(interceptor);
        });

        // TODO: Nơi này sẽ đăng ký StoreRepository và StoreUnitOfWork ở bước sau
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IStoreQueryService, StoreQueryService >();
        return services;
    }
}