using Shared.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace Shared.Infrastructure.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Biến phụ trợ lấy thông tin User từ Context
    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid UserId
    {
        get
        {
            if (!IsAuthenticated) return Guid.Empty;

            // Ưu tiên đọc từ Claim NameIdentifier hoặc Sub
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User?.FindFirst(JwtRegisteredClaimNames.Sub);

            return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var id)
                ? id
                : Guid.Empty;
        }
    }

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            if (!IsAuthenticated) return Array.Empty<string>();

            return User?.FindAll(ClaimTypes.Role)
                       .Select(c => c.Value)
                       .ToList()
                       .AsReadOnly() ?? new List<string>().AsReadOnly();
        }
    }

    public Guid? ActiveStoreId
    {
        get
        {
            if (!IsAuthenticated) return null;

            // Đọc StoreId từ Header do Frontend gửi lên (Context Switching)
            var storeIdHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Store-Id"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(storeIdHeader) && Guid.TryParse(storeIdHeader, out var storeId))
            {
                return storeId;
            }

            return null; // Trả về null nếu đây là lệnh điều hành toàn hệ thống hoặc Customer
        }
    }

    public bool IsInRole(string role)
    {
        return Roles.Contains(role);
    }

    public Guid? SessionId
    {
        get
        {
            if (!IsAuthenticated) return null;

            var sessionIdClaim = User?.FindFirst("SessionId");
            return sessionIdClaim != null && Guid.TryParse(sessionIdClaim.Value, out var sessionId)
                ? sessionId
                : null;
        }
    }
}