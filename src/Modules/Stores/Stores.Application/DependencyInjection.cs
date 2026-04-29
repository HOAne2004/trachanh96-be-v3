using Stores.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Stores.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddStoresApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(assembly);
                config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(StoresTransactionBehavior<,>));
            });
            services.AddValidatorsFromAssembly(assembly);

            return services;
        }
    }
}
