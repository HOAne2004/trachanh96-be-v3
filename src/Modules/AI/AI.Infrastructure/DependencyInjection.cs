using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AI.Infrastructure.Services;
using AI.Application.Interfaces;

namespace AI.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAIInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Đăng ký GeminiService làm IAIService
            services.AddHttpClient<IAIService, GeminiService>();
            return services;
        }
    }
}
