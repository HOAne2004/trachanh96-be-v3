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
using Shared.Application.Interfaces;
using Shared.Infrastructure.Interceptors;
using Shared.Infrastructure.Outbox;
using Shared.Infrastructure.Services;

namespace Identity.Infrastructure;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
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
                x => x.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName)
            ).AddInterceptors(auditInterceptor, outboxInterceptor);
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