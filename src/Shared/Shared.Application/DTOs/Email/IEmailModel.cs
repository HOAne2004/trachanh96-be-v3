namespace Shared.Application.DTOs.Email;

/// <summary>
/// Đánh dấu các Email Model có field CompanyName dùng chung - cho phép EmailService gán
/// giá trị này từ MỘT nguồn cấu hình duy nhất, thay vì lặp lại chuỗi cứng ở từng Model
/// (đã lệch chính tả "Trà Chanh 1996" vs "Trà Chanh 96" giữa các Model hiện tại - bằng
/// chứng cụ thể cho rủi ro của việc lặp literal). Không gộp toàn bộ cấu trúc Model vì mỗi
/// email vẫn phục vụ mục đích riêng, có thể phát triển khác nhau về sau.
/// </summary>
public interface IEmailModel
{
    string CompanyName { get; set; }
}