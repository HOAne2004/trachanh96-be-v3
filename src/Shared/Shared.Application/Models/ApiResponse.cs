// Nơi lưu: Shared.Application/Models/ApiResponse.cs
namespace Shared.Application.Models;

/// <summary>
/// [MÔ HÌNH: ĐỊNH DẠNG TRẢ VỀ CHUẨN CHO FRONTEND]
/// Chức năng: Đóng gói mọi kết quả thành công (HTTP 200) thành một cấu trúc JSON thống nhất.
/// Khớp hoàn toàn với interface ApiResponse<T> trong TypeScript của Vue.js.
/// </summary>
public class ApiResponse<T>
{
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Status { get; set; }
    public bool Success { get; set; }
}