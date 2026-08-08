using FluentEmail.Core;
using Shared.Application.DTOs.Email;
using Shared.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Shared.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly IFluentEmailFactory _fluentEmailFactory;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _templateBasePath;
    private readonly string _companyName;

    public EmailService(
        IFluentEmailFactory fluentEmailFactory,
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _fluentEmailFactory = fluentEmailFactory;
        _logger = logger;
        _configuration = configuration;
        _templateBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
        // Nguồn duy nhất cho tên công ty - sửa 1 chỗ, áp dụng cho mọi email.
        _companyName = configuration["EmailSettings:CompanyName"] ?? "Trà Chanh 1996";
    }

    public Task SendResetPasswordEmailAsync(string toEmail, string username, string token)
    {
        return SendEmailAsync(
            toEmail,
            subject: $"Mã OTP khôi phục mật khẩu - {_companyName}",
            templateName: "ResetPassword.cshtml",
            model: new ResetPasswordEmailModel { Username = username, Token = token, CompanyName = _companyName }
        );
    }

    public Task SendVerificationEmailAsync(string toEmail, string username, string token)
    {
        var frontendUrl = _configuration["FrontendSettings:BaseUrl"] ?? "http://localhost:3000";

        return SendEmailAsync(
            toEmail,
            subject: $"Xác thực tài khoản - {_companyName}",
            templateName: "VerifyEmail.cshtml",
            model: new VerifyEmailModel
            {
                Username = username,
                Email = toEmail,
                Token = token,
                FrontendUrl = frontendUrl,
                CompanyName = _companyName
            }
        );
    }

    public Task SendChangeEmailOtpAsync(string toEmail, string username, string token)
    {
        return SendEmailAsync(
            toEmail,
            subject: $"Mã OTP xác nhận thay đổi Email - {_companyName}",
            templateName: "ChangeEmailOtp.cshtml",
            model: new ChangeEmailOtpModel { Username = username, Token = token, CompanyName = _companyName }
        );
    }

    public Task SendWelcomeSetPasswordEmailAsync(string toEmail, string username, string token)
    {
        var frontendUrl = _configuration["FrontendSettings:BaseUrl"] ?? "http://localhost:3000";

        return SendEmailAsync(
            toEmail,
            subject: $"Chào mừng bạn đến với {_companyName} - Thiết lập mật khẩu",
            templateName: "Welcome.cshtml",
            model: new WelcomeSetPasswordEmailModel
            {
                Username = username,
                Email = toEmail,
                Token = token,
                FrontendUrl = frontendUrl,
                CompanyName = _companyName
            }
        );
    }

    private async Task SendEmailAsync<TModel>(string toEmail, string subject, string templateName, TModel model)
    {
        try
        {
            string templatePath = Path.Combine(_templateBasePath, templateName);

            var email = _fluentEmailFactory
                .Create()
                .To(toEmail)
                .Subject(subject)
                .UsingTemplateFromFile(templatePath, model);

            var response = await email.SendAsync();

            if (!response.Successful)
            {
                var errors = string.Join(", ", response.ErrorMessages);
                _logger.LogError($"[MAIL_FAILED] Gửi email '{subject}' thất bại đến {toEmail}. Lỗi: {errors}");
                throw new Exception($"Không thể gửi email: {errors}");
            }

            _logger.LogInformation($"[MAIL_SUCCESS] Đã gửi '{subject}' thành công đến: {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[MAIL_ERROR] Lỗi hệ thống khi gửi email '{subject}' đến {toEmail}");
            throw;
        }
    }
}