/// <summary>
/// [INFRASTRUCTURE SERVICE: GIAO TIẾP VỚI MÁY CHỦ EMAIL]
/// </summary>
using FluentEmail.Core;
using Shared.Application.DTOs.Email;
using Shared.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; // BỔ SUNG THƯ VIỆN NÀY

namespace Shared.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly IFluentEmailFactory _fluentEmailFactory;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration; // BỔ SUNG BIẾN NÀY
    private readonly string _templateBasePath;

    // TIÊM IConfiguration VÀO CONSTRUCTOR
    public EmailService(
        IFluentEmailFactory fluentEmailFactory,
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _fluentEmailFactory = fluentEmailFactory;
        _logger = logger;
        _configuration = configuration;
        // Khởi tạo base path 1 lần duy nhất ở Constructor
        _templateBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
    }

    public Task SendResetPasswordEmailAsync(string toEmail, string username, string token)
    {
        return SendEmailAsync(
            toEmail,
            subject: "Mã OTP khôi phục mật khẩu - Trà Chanh 1996",
            templateName: "ResetPassword.cshtml",
            model: new ResetPasswordEmailModel { Username = username, Token = token, CompanyName = "Trà Chanh 1996" }
        );
    }

    public Task SendVerificationEmailAsync(string toEmail, string username, string token)
    {
        // 1. Lấy URL của Frontend từ appsettings.json (Mặc định lấy localhost:3000 nếu chưa cấu hình)
        var frontendUrl = _configuration["FrontendSettings:BaseUrl"] ?? "http://localhost:3000";

        // 2. Truyền ĐẦY ĐỦ tham số cho Model
        return SendEmailAsync(
            toEmail,
            subject: "Xác thực tài khoản - Trà Chanh 1996",
            templateName: "VerifyEmail.cshtml",
            model: new VerifyEmailModel
            {
                Username = username,
                Email = toEmail,           // ĐÃ BỔ SUNG
                Token = token,
                FrontendUrl = frontendUrl, // ĐÃ BỔ SUNG
                CompanyName = "Trà Chanh 1996"
            }
        );
    }

    public Task SendChangeEmailOtpAsync(string toEmail, string username, string token)
    {
        return SendEmailAsync(
            toEmail,
            subject: "Mã OTP xác nhận thay đổi Email - Trà Chanh 1996",
            templateName: "ChangeEmailOtp.cshtml",
            model: new ChangeEmailOtpModel { Username = username, Token = token, CompanyName = "Trà Chanh 1996" }
        );
    }

    // =========================================================================
    // HÀM HELPER PRIVATE: GOM TẤT CẢ LOGIC I/O VÀ ERROR HANDLING VÀO ĐÂY
    // =========================================================================
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

                // Ném exception để Tầng Application có thể catch
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