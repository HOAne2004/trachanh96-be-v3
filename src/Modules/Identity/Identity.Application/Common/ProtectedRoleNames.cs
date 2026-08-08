namespace Identity.Application.Common;

/// <summary>
/// Danh sách NormalizedName của các Role được coi là "hệ thống cấp cao" - áp dụng ràng buộc
/// đặc biệt: không được gỡ khỏi người giữ cuối cùng, và chỉ người đang giữ Role này mới được
/// gán nó cho người khác. Dùng chung cho UpdateRolePermissionsCommand, AssignRolesToUserCommand,
/// CreateUserByAdminCommand để đảm bảo nhất quán.
/// </summary>
public static class ProtectedRoleNames
{
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ADMIN",
        "SUPER_ADMIN"
    };
}