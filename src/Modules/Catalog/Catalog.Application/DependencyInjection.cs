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
                config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CatalogTransactionBehavior<,>));
            });
            services.AddValidatorsFromAssembly(assembly);
            
            return services;
        }
    }
}
