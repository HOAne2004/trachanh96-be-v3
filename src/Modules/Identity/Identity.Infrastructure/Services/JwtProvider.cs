using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.Infrastructure.Services;

public class JwtProvider : IJwtProvider
{
    private readonly IConfiguration _config;

    public JwtProvider(IConfiguration config)
    {
        _config = config;
    }

    public string Generate(User user)
    {
        var secretKey = _config["JwtSettings:Key"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new ArgumentNullException("JwtSettings:Key không được cấu hình trong appsettings.json.");
        }

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.PublicId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email.Value),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(360), // Token sống 6 tiếng
            SigningCredentials = creds,
            Issuer = _config["JwtSettings:Issuer"],
            Audience = _config["JwtSettings:Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}