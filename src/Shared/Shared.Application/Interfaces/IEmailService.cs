namespace Shared.Application.Interfaces;

public interface IEmailService
{
    Task SendResetPasswordEmailAsync(string toEmail, string username, string token);
    Task SendVerificationEmailAsync(string toEmail, string username, string token);
}