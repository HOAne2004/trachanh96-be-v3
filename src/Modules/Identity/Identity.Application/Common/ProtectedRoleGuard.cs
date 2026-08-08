using Identity.Application.Interfaces;

namespace Identity.Application.Common;

/// <summary>
/// Kiểm tra dùng chung cho các thao tác có thể khiến 1 User mất quyền truy cập
/// (xóa, khóa, gỡ Role) - đảm bảo không làm 1 Role hệ thống (ADMIN/SUPER_ADMIN) mất
/// người giữ cuối cùng, dẫn đến không ai còn quyền quản trị hệ thống.
/// </summary>
public static class ProtectedRoleGuard
{
    public static async Task<string?> CheckLastHolderViolationAsync(
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        Guid userId,
        IEnumerable<Guid> roleIdsBeingRemoved,
        CancellationToken cancellationToken)
    {
        var roles = await roleRepository.GetRolesByIdsAsync(roleIdsBeingRemoved, cancellationToken);
        var protectedRoles = roles.Where(r => ProtectedRoleNames.Names.Contains(r.NormalizedName)).ToList();

        foreach (var role in protectedRoles)
        {
            var remainingHolders = await userRepository.CountUsersInRoleAsync(role.Id, excludeUserId: userId, cancellationToken);
            if (remainingHolders == 0)
            {
                return $"Không thể thực hiện vì đây là người dùng cuối cùng đang giữ vai trò hệ thống '{role.Name}'.";
            }
        }

        return null;
    }
}