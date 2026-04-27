using Orders.Application.Interfaces.ExternalServices;

namespace Orders.Infrastructure.ExternalServices;

public class PaymentGatewayService : IPaymentGatewayService
{
    public Task<PaymentLinkResponse> GeneratePaymentUrlAsync(
        Guid orderId, string orderCode, decimal amount,
        Guid paymentMethodId, CancellationToken cancellationToken)
    {
        // Giả lập tạo URL thanh toán. Trên thực tế sẽ gọi thư viện VNPay hoặc MoMo
        string dummyUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount={amount * 100}&vnp_TxnRef={orderCode}";
        string dummyTransactionId = Guid.NewGuid().ToString("N");

        return Task.FromResult(new PaymentLinkResponse(dummyUrl, dummyTransactionId));
    }
}