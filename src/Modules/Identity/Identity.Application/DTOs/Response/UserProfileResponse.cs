
namespace Identity.Application.DTOs.Response
{
    public record UserProfileResponse(
        Guid Id,
        string Email,
        string FullName,
        IList<string> Roles,
        string? Phone,
        string? ThumbnailUrl,
        bool EmailVerified
    );
}
