using MediatR;
using FluentValidation;
using System.Reflection;
using Orders.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;


namespace Orders.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddOrdersApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(assembly);
                config.AddOpenBehavior(typeof(Shared.Application.Behaviors.ValidationBehavior<,>));
                config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(OrdersTransactionBehavior<,>));
            });
            services.AddValidatorsFromAssembly(assembly);
            return services;
        }
    }
}
