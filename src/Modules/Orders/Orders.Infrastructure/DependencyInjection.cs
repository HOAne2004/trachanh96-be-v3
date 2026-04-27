using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Interfaces;
using Orders.Application.Interfaces.ExternalServices;
using Orders.Infrastructure.BackgroundJobs;
using Orders.Infrastructure.Database;
using Orders.Infrastructure.ExternalServices;
using Orders.Infrastructure.Repositories;
using Shared.Infrastructure.Interceptors;
using Shared.Infrastructure.Outbox;

namespace Orders.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Đăng ký Interceptors vào DI Container
            services.AddScoped<AuditableEntityInterceptor>();
            services.AddScoped<InsertOutboxMessagesInterceptor>(); // Đăng ký trước khi gọi AddDbContext

            // Đăng ký OrdersDbContext
            services.AddDbContext<OrdersDbContext>((sp, options) =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

                // Resolve các Interceptors từ DI
                var auditableInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                var outboxInterceptor = sp.GetRequiredService<InsertOutboxMessagesInterceptor>(); // Lấy ra

                // Đăng ký CẢ 2 VÀO EF CORE (Thứ tự không quá quan trọng, nhưng nên để Outbox sau)
                options.AddInterceptors(auditableInterceptor, outboxInterceptor);
            });
            
            services.AddScoped<IOrdersDbContext>(provider => provider.GetRequiredService<OrdersDbContext>());
            
            services.AddScoped<IOrderRepository, OrderRepository>();
            
            services.AddHostedService<ProcessOutboxMessagesJob>();
            services.AddHostedService<OrderAutoCancelWorker>();

            services.AddScoped<IOrdersUnitOfWork, OrdersUnitOfWork>();
            services.AddScoped<IProductSnapshotService, ProductSnapshotService>();
            services.AddScoped<IVoucherSnapshotService, VoucherSnapshotService>();
            services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();
            services.AddScoped<IUserAddressService, UserAddressService>();
            services.AddScoped<IStoreTableService, StoreTableService>();
            services.AddScoped<IStoreDeliveryPolicyService, StoreDeliveryPolicyService>();
            
            return services;
        }
    }
}