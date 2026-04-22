using Catalog.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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
            services.AddScoped(
                typeof(IPipelineBehavior<,>),
                typeof(CatalogTransactionBehavior<,>)
            );
            return services;
        }
    }
}
