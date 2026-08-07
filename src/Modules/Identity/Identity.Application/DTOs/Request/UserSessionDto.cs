
namespace Identity.Application.DTOs.Request
{
    public record UserSessionDto(
        Guid SessionId,
        string DeviceName,
        string IpAddress,
        DateTime ExpiryDate,
        DateTime CreatedAt,
        bool IsCurrentSession);
}
