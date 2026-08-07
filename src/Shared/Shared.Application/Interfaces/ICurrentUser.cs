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

    /// <summary>
    /// SessionId gắn với access token hiện tại (đọc từ claim "SessionId").
    /// Null nếu chưa đăng nhập hoặc token không mang claim này (VD: token cũ phát hành
    /// trước khi tính năng này được thêm - sẽ tự hết claim sau khi access token cũ hết hạn,
    /// thường tối đa AccessTokenExpirationMinutes phút).
    /// </summary>
    Guid? SessionId { get; }

}