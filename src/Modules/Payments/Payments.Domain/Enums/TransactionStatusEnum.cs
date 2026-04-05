namespace Payments.Domain.Enums;

public enum TransactionStatusEnum
{
    Pending = 1,    // Đang chờ khách quét mã
    Success = 2,    // Thành công
    Failed = 3,     // Lỗi (Thẻ hết tiền, ngân hàng từ chối)
    Expired = 4,    // Quá thời gian thanh toán (VD: 15 phút), link hết hạn
    Refunded = 5    // Đã hoàn tiền lại cho khách
}