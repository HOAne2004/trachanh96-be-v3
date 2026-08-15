namespace Shared.Application.Interfaces;

public interface IEmailService
{
    Task SendResetPasswordEmailAsync(string toEmail, string username, string token);
    Task SendVerificationEmailAsync(string toEmail, string username, string token);
    Task SendChangeEmailOtpAsync(string toEmail, string username, string token);
    /// <summary>
    /// Gửi email mời thiết lập mật khẩu lần đầu cho tài khoản do Admin tạo. Token là một
    /// link dài (không cần gõ tay) - khác hẳn OTP 6 số của SendResetPasswordEmailAsync,
    /// vì token này không do người dùng chủ động yêu cầu nên không cần dễ đọc/gõ.
    /// </summary>
    Task SendWelcomeSetPasswordEmailAsync(string toEmail, string username, string token);
    Task SendAccountLockedEmailAsync(string toEmail, string username, string reason, DateTime lockoutEnd);
}