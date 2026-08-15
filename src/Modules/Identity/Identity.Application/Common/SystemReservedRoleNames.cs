namespace Identity.Application.Common;

/// <summary>
/// Danh sách NormalizedName của các Role mà hệ thống PHỤ THUỘC CỨNG vào tên (tra cứu bằng
/// GetByNormalizedNameAsync ở nơi khác, VD: RegisterUserCommand tra "CUSTOMER" để gán Role mặc định).
/// Đổi tên hoặc xóa các Role này sẽ làm gãy chức năng liên quan một cách ÂM THẦM (không exception).
/// Superset của ProtectedRoleNames (ADMIN/SUPER_ADMIN) - "CUSTOMER" cũng cần bảo vệ khỏi
/// đổi tên/xóa nhưng KHÔNG mang ý nghĩa "leo thang đặc quyền", nên tách riêng khỏi ProtectedRoleNames
/// để không ảnh hưởng logic escalation-check ở AssignRolesToUserCommand/CreateUserByAdminCommand.
/// </summary>
public static class SystemReservedRoleNames
{
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ADMIN",
        "SUPER_ADMIN",
        "CUSTOMER"
    };
}