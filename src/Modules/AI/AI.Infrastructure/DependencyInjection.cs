using AI.Application.Interfaces;
using AI.Infrastructure.Database;
using AI.Infrastructure.Repositories;
using AI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAIInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Đăng ký Database
            services.AddDbContext<AIDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // 2. Đăng ký Repository
            services.AddScoped<IChatRepository, ChatRepository>();

            // 3. Đăng ký HttpClient với chuẩn Resilience của .NET 8
            services.AddHttpClient<IAIService, GeminiService>()
                    .AddStandardResilienceHandler(options =>
                    {
                        // Bạn có thể để trống () để dùng cấu hình mặc định (Retry 3 lần, Circuit Breaker...), 
                        // Cấu hình mặc định này đã cực kỳ tối ưu cho các request gọi LLM.
                    });

            return services;
        }
    }
}