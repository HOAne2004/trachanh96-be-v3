using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Payments.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 1. Dạy MediatR quét tất cả các Command/Query/Handler trong thư mục này
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // 2. Dạy FluentValidation quét tất cả các Validator trong thư mục này
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}