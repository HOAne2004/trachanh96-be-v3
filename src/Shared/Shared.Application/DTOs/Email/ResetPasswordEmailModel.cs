namespace Shared.Application.DTOs.Email;

public class ResetPasswordEmailModel : IEmailModel
{
    public string Username { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}