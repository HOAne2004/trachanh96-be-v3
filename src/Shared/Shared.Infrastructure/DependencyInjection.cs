using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Interfaces;
using Shared.Infrastructure.Interceptors;
using Shared.Infrastructure.Outbox;
using Shared.Infrastructure.Storage;
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

        // 1. Lấy thông tin Supabase từ appsettings.json
        var supabaseUrl = configuration["Supabase:Url"]
                ?? throw new InvalidOperationException("Configuration value 'Supabase:Url' is required.");
        var supabaseKey = configuration["Supabase:Key"];

        // 2. Khởi tạo Supabase Client (Nên đăng ký dạng Singleton vì nó quản lý connection)
        var options = new Supabase.SupabaseOptions
        {
            AutoConnectRealtime = true // Tùy chọn, false nếu bạn chỉ dùng Storage
        };

        services.AddSingleton(provider => new Supabase.Client(supabaseUrl, supabaseKey, options));

        // 3. Đăng ký IStorageService (Nối Interface với Class thực thi)
        services.AddScoped<IStorageService, SupabaseStorageService>();

        return services;
    }
}