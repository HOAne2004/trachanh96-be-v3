namespace Shared.Application.DTOs.Email;

public class AccountLockedEmailModel : IEmailModel
{
    public string Username { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime LockoutEnd { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}