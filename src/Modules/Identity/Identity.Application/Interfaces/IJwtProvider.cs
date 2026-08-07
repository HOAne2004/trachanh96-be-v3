using Identity.Application.Models;

namespace Identity.Application.Interfaces;

public interface IJwtProvider
{
    string GenerateAccessToken(TokenUser user); 
    string GenerateRefreshToken();
    DateTime GetRefreshTokenExpiry();
    DateTime GetAccessTokenExpiry();
    string HashToken(string token);
}