/// <summary>
/// [MÔ HÌNH: DATA TRANSFER OBJECT (DTO) CHUẨN HÓA LỖI]
/// Chức năng: Tạo ra một định dạng JSON thống nhất cho mọi lỗi trả về từ API xuống Client (Vue, Mobile).
/// 
/// Cách hoạt động:
/// - Sử dụng record (immutable): Dữ liệu lỗi được tạo ra một lần và không thể bị sửa đổi ở giữa đường.
/// - ErrorCode: Mã lỗi dạng chuỗi (VD: "USER_NOT_FOUND", "INVALID_EMAIL") giúp Frontend dễ dùng if/else hoặc switch case để dịch ngôn ngữ.
/// - Message: Câu thông báo thân thiện cho người dùng.
/// - Details: Mảng chứa các chi tiết lỗi cụ thể (Rất hữu ích để chứa danh sách lỗi quét được từ FluentValidation ở ValidationBehavior).
/// 
/// Sử dụng: Dùng chủ yếu ở tầng API (trong Global Exception Handler hoặc BaseApiController) để bọc Result.Error lại trước khi bắn HTTP Response qua mạng.
/// </summary>

namespace Shared.Application.Models;

public record ErrorResponse(
    string ErrorCode,
    string Message,
    IEnumerable<string>? Details = null
);