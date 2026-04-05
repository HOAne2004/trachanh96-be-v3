using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payments.Infrastructure.BackgroundJobs;
using Payments.Infrastructure.Database;
using Shared.Infrastructure.Interceptors;
using Shared.Infrastructure.Outbox;

namespace Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Lưu ý: Không cần đăng ký lại AddScoped cho Interceptors vì ở Shared.Infrastructure đã đăng ký rồi!

        // Đăng ký PaymentsDbContext
        services.AddDbContext<PaymentsDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            // Gọi 2 điệp viên (Interceptors) ra làm việc
            var auditableInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            var outboxInterceptor = sp.GetRequiredService<InsertOutboxMessagesInterceptor>();

            // Cắm điệp viên vào DbContext
            options.AddInterceptors(auditableInterceptor, outboxInterceptor);
        });

        // Đăng ký Background Worker chạy ngầm
        services.AddHostedService<PaymentOutboxWorker>();

        // Tương lai: Đăng ký Repository, VNPayService...
        // services.AddScoped<IPaymentRepository, PaymentRepository>();

        return services;
    }
}