using FluentValidation;
using Identity.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Đăng ký MediatR và Pipeline Behavior
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // Đăng ký FluentValidation
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped(
            typeof(IPipelineBehavior<,>),
            typeof(IdentityTransactionBehavior<,>)
        );
        return services;
    }
}