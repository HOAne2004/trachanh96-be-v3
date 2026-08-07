
namespace Identity.Application.DTOs.Request
{
    public record UserAdminDto(
    Guid Id, // Chuẩn Guid V7
    string Email,
    string FullName,
    string? Phone,
    IList<string> Roles, // Đổi thành danh sách các quyền (VD: ["Admin", "Manager"])
    string Status,
    DateTime CreatedAt
);
}
