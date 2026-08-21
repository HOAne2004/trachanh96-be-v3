using Identity.Application;
using Identity.Application.Interfaces;
using Identity.Application.Settings;
using Identity.Infrastructure.Authorization;
using Identity.Infrastructure.Database;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Interceptors;
using Shared.Infrastructure.Outbox;

using Microsoft.Extensions.Logging;
namespace Identity.Infrastructure;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // 1. Đăng ký Tầng Application (MediatR, FluentValidation...)
        services.AddIdentityApplication();

        // 2. Cấu hình Options (Map JwtSettings từ appsettings.json vào class JwtOptions)
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // 3. Đăng ký Interceptors
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<InsertOutboxMessagesInterceptor>();

        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            var auditInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            var outboxInterceptor = sp.GetRequiredService<InsertOutboxMessagesInterceptor>();

            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(15), errorCodesToAdd: null);
                }
            ).AddInterceptors(auditInterceptor, outboxInterceptor);

            // CHỈ bật ở Development: in ra chính xác câu SQL EF Core gửi đi, kèm tham số thật -
            // để thấy trực tiếp UPDATE nào đang chạy, WHERE điều kiện gì, thay vì đoán mò.
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.LogTo(Console.WriteLine, LogLevel.Information);
            }
        });

        // 4. Đăng ký Background Job xử lý Outbox (dùng bản generic dùng chung ở Shared.Infrastructure.Outbox)
        services.AddHostedService<ProcessOutboxMessagesJob<IdentityDbContext>>();

        // 5. Đăng ký các Services & Repositories lõi
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IIdentityUnitOfWork, IdentityUnitOfWork>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();

        // 6. Đăng ký Hệ thống Phân quyền (Authorization Framework)
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}