using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Behaviors; // Trỏ đến ValidationBehavior của bạn
using System.Reflection;

namespace AI.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAIApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Đăng ký toàn bộ Validator trong tầng Application
            services.AddValidatorsFromAssembly(assembly);

            // Đăng ký MediatR và chèn ValidationBehavior làm "bảo vệ cửa"
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            return services;
        }
    }
}