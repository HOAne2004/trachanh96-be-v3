using FluentEmail.Core;
using Shared.Application.DTOs.Email;
using Shared.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly IFluentEmailFactory _fluentEmailFactory;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IFluentEmailFactory fluentEmailFactory, ILogger<EmailService> logger)
    {
        _fluentEmailFactory = fluentEmailFactory;
        _logger = logger;
    }

    public async Task SendResetPasswordEmailAsync(string toEmail, string username, string token)
    {
        try
        {
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "ResetPassword.cshtml");

            var email = _fluentEmailFactory
                .Create()
                .To(toEmail)
                .Subject("Mã OTP khôi phục mật khẩu - Trà Chanh 96")
                .UsingTemplateFromFile(templatePath, new ResetPasswordEmailModel
                {
                    Username = username,
                    Token = token,
                    CompanyName = "Trà Chanh 96"
                });

            var response = await email.SendAsync();

            if (!response.Successful)
            {
                var errors = string.Join(", ", response.ErrorMessages);
                _logger.LogError($"Gửi email reset pass thất bại đến {toEmail}. Lỗi: {errors}");
                throw new Exception($"Không thể gửi email: {errors}");
            }

            _logger.LogInformation($"[SUCCESS] Đã gửi mail OTP thành công đến: {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi hệ thống khi gửi email reset pass đến {toEmail}");
            throw;
        }
    }

    public async Task SendVerificationEmailAsync(string toEmail, string username, string token)
    {
        try
        {
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "VerifyEmail.cshtml");

            var email = _fluentEmailFactory
                .Create()
                .To(toEmail)
                .Subject("Xác thực tài khoản - Trà Chanh 96")
                .UsingTemplateFromFile(templatePath, new VerifyEmailModel
                {
                    Username = username,
                    Token = token,
                    VerificationLink = "", // Bỏ trống vì ta dùng OTP
                    CompanyName = "Trà Chanh 96"
                });

            var response = await email.SendAsync();

            if (!response.Successful)
            {
                var errors = string.Join(", ", response.ErrorMessages);
                _logger.LogError($"Gửi email xác thực thất bại đến {toEmail}. Lỗi: {errors}");
                throw new Exception($"Không thể gửi email xác thực: {errors}");
            }

            _logger.LogInformation($"[SUCCESS] Đã gửi mail xác thực thành công đến: {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi hệ thống khi gửi email xác thực đến {toEmail}");
            throw;
        }
    }
}