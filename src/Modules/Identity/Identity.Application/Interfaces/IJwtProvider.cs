using Identity.Domain.Entities;
namespace Identity.Application.Interfaces
{
    public interface IJwtProvider
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        DateTime GetRefreshTokenExpiry();
    }
}
