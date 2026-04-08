
using MediatR;
using Payments.Application.Interfaces;
using Payments.Domain.Enums;
using Shared.Application.Models;
using System.Text.Json;

namespace Payments.Application.Features.Commands
{
    public record ProcessVnPayCallbackCommand(
        Dictionary<string, string> ResponseData) : IRequest<Result<string>>;

    public class ProcessVnPayCallbackCommandHandler : IRequestHandler<ProcessVnPayCallbackCommand, Result<string>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IVnPayService _vnPayService;

        public ProcessVnPayCallbackCommandHandler(IPaymentRepository paymentRepository, IVnPayService vnPayService)
        {
            _paymentRepository = paymentRepository;
            _vnPayService = vnPayService;
        }

        public async Task<Result<string>> Handle(ProcessVnPayCallbackCommand request, CancellationToken cancellationToken)
        {
            var responseData = request.ResponseData;

            // 1. Kiểm tra tính hợp lệ của Chữ ký (Chống hacker giả mạo URL)
            if (!_vnPayService.IsValidSignature(responseData))
            {
                return Result<string>.Failure("Chữ ký VNPay không hợp lệ. Dữ liệu có thể đã bị can thiệp!");
            }

            // 2. Lấy các thông tin quan trọng từ VNPay trả về
            // vnp_TxnRef chính là IdempotencyKey mà ta gửi đi lúc nãy
            string txnRef = responseData.GetValueOrDefault("vnp_TxnRef") ?? string.Empty;
            string responseCode = responseData.GetValueOrDefault("vnp_ResponseCode") ?? string.Empty;
            string transactionNo = responseData.GetValueOrDefault("vnp_TransactionNo") ?? string.Empty; // Mã GD của VNPay

            // Gói toàn bộ cục data lại thành chuỗi JSON để lưu log (GatewayResponse)
            string rawResponse = JsonSerializer.Serialize(responseData);

            // 3. Tìm giao dịch trong Database (Lưu ý: Bạn cần thêm hàm GetByIdempotencyKeyAsync vào IPaymentRepository nếu chưa có)
            if (!Guid.TryParse(txnRef, out Guid idempotencyKey))
                return Result<string>.Failure("Mã giao dịch tham chiếu không hợp lệ.");

            var transaction = await _paymentRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);

            if (transaction == null)
                return Result<string>.Failure("Không tìm thấy giao dịch trong hệ thống.");

            // 4. Nếu giao dịch đã được xử lý trước đó rồi (bởi Webhook IPN) thì bỏ qua
            if (transaction.Status != TransactionStatusEnum.Pending)
                return Result<string>.Success("Giao dịch này đã được xử lý trước đó.");

            // 5. Cập nhật trạng thái dựa vào ResponseCode của VNPay ("00" là thành công)
            if (responseCode == "00")
            {
                transaction.MarkAsGatewaySuccess(transactionNo, rawResponse);
            }
            else
            {
                // VNPay trả về mã lỗi (Ví dụ: 24 - Khách hàng hủy giao dịch)
                transaction.MarkAsGatewayFailed($"VNPay Response Code: {responseCode}", rawResponse);
            }

            // (TransactionBehavior của chúng ta sẽ tự động gọi SaveChangesAsync ở bước này)

            return Result<string>.Success($"Xử lý giao dịch {transaction.OrderCode} hoàn tất.");
        }
    }
}
