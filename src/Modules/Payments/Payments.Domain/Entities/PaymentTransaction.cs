using Payments.Domain.Enums;
using Payments.Domain.Events;
using Shared.Domain;
using Shared.Domain.Exceptions;
using Shared.Domain.Interfaces;
using Shared.Domain.SeedWork;

namespace Payments.Domain.Entities;

public class PaymentTransaction : AggregateRoot<Guid>, IAuditableEntity
{
    // ==========================================
    // 1. LIÊN KẾT & CHỐNG SPAM
    // ==========================================
    public Guid OrderId { get; private set; }
    public string OrderCode { get; private set; }
    public Guid IdempotencyKey { get; private set; }

    // ==========================================
    // 2. THÔNG TIN TIỀN BẠC
    // ==========================================
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public PaymentMethodEnum PaymentMethod { get; private set; }

    // ==========================================
    // 3. THEO DÕI VÀ ĐỐI SOÁT (AUDIT)
    // ==========================================
    public TransactionStatusEnum Status { get; private set; }
    public string? GatewayTransactionId { get; private set; }
    public string? GatewayResponse { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? ConfirmedBy { get; private set; }

    // ==========================================
    // 4. HOÀN TIỀN (REFUND)
    // ==========================================
    public string? RefundTransactionId { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    // ==========================================
    // 5. THỜI GIAN & ĐỒNG BỘ
    // ==========================================
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime ExpiredAt { get; private set; }
    public byte[] RowVersion { get; private set; }

    // Constructor rỗng cho EF Core
    protected PaymentTransaction()
    {
        OrderCode = null!;
        Currency = null!;
        RowVersion = Array.Empty<byte>();
    }

    private PaymentTransaction(Guid orderId, string orderCode, decimal amount, string currency, PaymentMethodEnum method, Guid idempotencyKey)
    {
        if (amount <= 0) throw new DomainException("Số tiền thanh toán phải lớn hơn 0.");

        if (string.IsNullOrWhiteSpace(currency) || currency.ToUpperInvariant() != "VND")
            throw new DomainException("Hệ thống hiện tại chỉ hỗ trợ tiền tệ là VND.");

        Id = Guid.NewGuid();
        OrderId = orderId;
        OrderCode = orderCode;
        Amount = amount;
        Currency = currency.ToUpperInvariant();
        PaymentMethod = method;
        IdempotencyKey = idempotencyKey;

        Status = TransactionStatusEnum.Pending;
        RowVersion = Array.Empty<byte>();

        CreatedAt = DateTime.UtcNow;
        ExpiredAt = CreatedAt.AddMinutes(15);
    }

    public static PaymentTransaction Create(Guid orderId, string orderCode, decimal amount, string currency, PaymentMethodEnum method, Guid idempotencyKey)
    {
        return new PaymentTransaction(orderId, orderCode, amount, currency, method, idempotencyKey);
    }

    // --- CÁC HÀM BEHAVIOR (Đã được review và chuẩn hóa ở trên) ---
    public void MarkAsCashReceived(Guid staffId)
    {
        if (PaymentMethod != PaymentMethodEnum.Cash) throw new DomainException("Giao dịch không phải tiền mặt.");
        if (Status != TransactionStatusEnum.Pending) throw new DomainException("Giao dịch không ở trạng thái chờ.");

        Status = TransactionStatusEnum.Success;
        CompletedAt = DateTime.UtcNow;
        ConfirmedBy = staffId;
        GatewayTransactionId = $"CASH-{Id.ToString()[..8].ToUpper()}";

        AddDomainEvent(new PaymentSucceededDomainEvent(OrderId, GatewayTransactionId, PaymentMethod));
    }

    public void MarkAsGatewaySuccess(string gatewayTransactionId, string rawResponse)
    {
        if (Status == TransactionStatusEnum.Success) return;

        if (string.IsNullOrWhiteSpace(gatewayTransactionId))
            throw new DomainException("GatewayTransactionId từ đối tác không được để trống.");

        if (Status == TransactionStatusEnum.Expired)
        {
            // Cứu vãn giao dịch Webhook trễ
            GatewayResponse = $"{GatewayResponse} || [LATE-SUCCESS-WARNING]: {rawResponse}";
            return;
        }

        if (Status != TransactionStatusEnum.Pending)
            throw new DomainException($"Không thể ghi nhận thành công vì giao dịch đang ở trạng thái {Status}.");

        Status = TransactionStatusEnum.Success;
        GatewayTransactionId = gatewayTransactionId;
        GatewayResponse = rawResponse;
        CompletedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentSucceededDomainEvent(OrderId, gatewayTransactionId, PaymentMethod));
    }

    public void MarkAsGatewayFailed(string errorMessage, string rawResponse)
    {
        if (Status == TransactionStatusEnum.Failed || Status == TransactionStatusEnum.Expired) return;

        if (Status != TransactionStatusEnum.Pending)
            throw new DomainException("Chỉ có thể đánh dấu lỗi cho giao dịch đang ở trạng thái chờ.");

        Status = TransactionStatusEnum.Failed;
        ErrorMessage = errorMessage;
        GatewayResponse = rawResponse;
        CompletedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentFailedDomainEvent(OrderId, errorMessage));
    }

    public void MarkAsExpired()
    {
        if (Status != TransactionStatusEnum.Pending) return;

        Status = TransactionStatusEnum.Expired;
        ErrorMessage = "Quá thời gian thanh toán (15 phút), giao dịch tự động bị hủy.";
        CompletedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentExpiredDomainEvent(OrderId));
    }

    public void MarkAsRefunded(string refundTransactionId, string rawResponse)
    {
        if (Status != TransactionStatusEnum.Success)
            throw new DomainException("Chỉ có thể hoàn tiền cho giao dịch đã thành công.");

        if (string.IsNullOrWhiteSpace(refundTransactionId))
            throw new DomainException("Mã giao dịch hoàn tiền không được để trống.");

        Status = TransactionStatusEnum.Refunded;
        RefundTransactionId = refundTransactionId;
        RefundedAt = DateTime.UtcNow;
        GatewayResponse = $"{GatewayResponse} || [REFUND]: {rawResponse}";

        AddDomainEvent(new PaymentRefundedDomainEvent(OrderId, refundTransactionId));
    }
}