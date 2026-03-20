namespace Shared.Application.DTOs.Email;

public class VerifyEmailModel
{
    public string Username { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string VerificationLink { get; set; } = string.Empty; 
    public string CompanyName { get; set; } = "Trà Chanh 96";
}