using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Behaviors;

namespace Shared.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedApplication(this IServiceCollection services)
    {
        // Đăng ký Idempotent Pipeline Behavior vào MediatR
        // Cú pháp AddTransient này tương thích với cơ chế tự động quét Behavior của MediatR
        // Tương lai nếu có thêm LoggingBehavior, ValidationBehavior... thì đăng ký tiếp ở đây
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotentCommandBehavior<,>));

        return services;
    }
}