using Identity.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace Identity.Infrastructure.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    // Phải tiêm IServiceProvider thay vì DbContext trực tiếp vì Handler này là Singleton
    public PermissionAuthorizationHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // 1. Lấy UserId từ JWT Token (Claim: sub)
        var userIdClaim = context.User.Claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.NameIdentifier ||
            c.Type == JwtRegisteredClaimNames.Sub);

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return; // Đuổi ra, trả về 401/403
        }

        // 2. Mở một scope mới để gọi DbContext
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        // 3. Truy vấn DB xem User này có Role chứa Permission được yêu cầu không
        // Logic: User -> UserRoles -> Role -> RolePermissions -> Permission == requirement.Permission
        var hasPermission = await dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(dbContext.RolePermissions,
                  ur => ur.RoleId,
                  rp => rp.RoleId,
                  (ur, rp) => rp)
            .AnyAsync(rp => rp.PermissionId == requirement.Permission);

        // 4. Ra quyết định
        if (hasPermission)
        {
            context.Succeed(requirement); // Duyệt cho qua!
        }
    }
}