
namespace Identity.Application.DTOs.Response
{
    public record LoginResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    DateTime RefreshTokenExpiry,
    Guid UserId,
    string Email,
    string FullName,
    IList<string> Roles,
    string? ThumbnailUrl
);
}
