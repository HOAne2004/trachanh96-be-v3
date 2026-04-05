using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Interfaces;
using Shared.Infrastructure.Interceptors;
using Shared.Infrastructure.Outbox;
using System.Net.Mail;

namespace Shared.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<InsertOutboxMessagesInterceptor>();
        // CẤU HÌNH FLUENT EMAIL
        var emailSettings = configuration.GetSection("EmailSettings");
        services.AddFluentEmail(emailSettings["DefaultFromEmail"] ?? "admin@example.com")
            .AddRazorRenderer()
            .AddSmtpSender(() => new SmtpClient(emailSettings["SmtpServer"] ?? "smtp.gmail.com")
            {
                Port = int.Parse(emailSettings["Port"] ?? "587"),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(
                    emailSettings["Username"],
                    emailSettings["Password"]?.Replace(" ", "")
                ),
                EnableSsl = true,
            });

        services.AddScoped<IEmailService, Email.EmailService>();

        return services;
    }
}