namespace Identity.Application.DTOs.Response
{
    public record UserSessionDto(
        Guid SessionId,
        string DeviceName,
        string IpAddress,
        DateTime ExpiryDate,
        DateTime CreatedAt,
        bool IsCurrentSession);
}
