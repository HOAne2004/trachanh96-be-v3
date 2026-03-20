using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(assembly);
                config.AddOpenBehavior(typeof(Shared.Application.Behaviors.ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(assembly);
            return services;
        }
    }
}
