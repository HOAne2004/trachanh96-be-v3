
namespace Payments.Infrastructure.Database.Configurations
{
    public class VnPayConfig
    {
        public const string ConfigName = "VnPayConfig";

        public string TmnCode { get; set; } = string.Empty; // Mã website tại VnPay cung cấp
        public string HashSecret { get; set; } = string.Empty; // Chuỗi bí mật dùng để tạo chữ ký (HMAC SHA256)
        public string VnPayUrl { get; set; } = string.Empty; // URL của cổng thanh toán VnPay
        public string ReturnUrl { get; set; } = string.Empty; // URL VnPay sẽ redirect về sau khi thanh toán xong
    }
}
