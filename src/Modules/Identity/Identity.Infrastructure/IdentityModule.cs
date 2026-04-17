using Identity.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityApplication(); // Đăng ký MediatR, Validation 
        services.AddIdentityInfrastructure(configuration); // Đăng ký DbContext, Repository 
        return services;
    }
}