
namespace Identity.Application.DTOs.Request
{
    public record UserDetailsAdminDto(
        Guid Id,
        string Email,
        string FullName,
        string? Phone,
        string? ThumbnailUrl,
        IList<string> Roles,
        string Status,
        bool EmailVerified,
        int FailedLoginAttempts,
        DateTime? LockoutEnd,
        DateTime CreatedAt
    );
}
