using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Application.Interfaces.ExternalServices;

// DTO chứa kết quả trả về cho Mobile App/Web
public record PaymentLinkResponse(
    string PaymentUrl, // Link để mở ra VNPay/Momo
    string TransactionId // Mã giao dịch nội bộ để sau này Webhook đối chiếu
);

public interface IPaymentGatewayService
{
    // Lệnh này sẽ gọi sang Module Payment (hoặc gọi thẳng API nếu bạn làm Monolith nguyên khối)
    Task<PaymentLinkResponse> GeneratePaymentUrlAsync(
        Guid orderId,
        string orderCode,
        decimal amount,
        Guid paymentMethodId, // Để biết là VNPay hay Momo mà tạo link cho đúng
        CancellationToken cancellationToken);
}
