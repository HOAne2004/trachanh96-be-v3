namespace Shared.Application.Interfaces;

public interface ICurrentUser
{
    /// <summary>
    /// ID của người dùng hiện tại (Lấy từ Claim Subject của JWT).
    /// Trả về Guid.Empty nếu chưa đăng nhập.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Danh sách các Role của người dùng.
    /// </summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>
    /// ID của chi nhánh (Store) hiện hành mà user đang làm việc.
    /// Giá trị này có thể thay đổi trong quá trình sử dụng mà không cần cấp lại Token.
    /// </summary>
    Guid? ActiveStoreId { get; }

    /// <summary>
    /// Cờ kiểm tra xem Request có chứa Token hợp lệ hay không.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Hàm tiện ích kiểm tra nhanh Role.
    /// </summary>
    bool IsInRole(string role);
}