using FluentValidation;
using MediatR;
using Orders.Application.Interfaces;
using Orders.Application.Interfaces.ExternalServices;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Orders.Application.Features.Commands
{
    // ==========================================
    // 1. Command
    // ==========================================
    public record CheckoutResultResponse(
    Guid OrderId,
    string? PaymentUrl // Nếu chọn tiền mặt (Cash) thì không có link này
);
    public record CheckoutOrderCommand(
    Guid OrderId,
    Guid PaymentMethodId,
    Guid IdempotencyKey 
) : IIdempotentCommand<Result<CheckoutResultResponse>>;

    // ==========================================
    // 2. Validator
    // ==========================================
    public class CheckoutOrderCommandValidator : AbstractValidator<CheckoutOrderCommand>
    {
        public CheckoutOrderCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty().WithMessage("Mã đơn hàng không hợp lệ.");
            RuleFor(x => x.PaymentMethodId).NotEmpty().WithMessage("Vui lòng chọn phương thức thanh toán.");
        }
    }

    // ==========================================
    // 3. Handler
    // ==========================================
    public class CheckoutOrderCommandHandler : IRequestHandler<CheckoutOrderCommand, Result<CheckoutResultResponse>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPaymentGatewayService _paymentGatewayService;

        public CheckoutOrderCommandHandler(
            IOrderRepository orderRepository,
            IPaymentGatewayService paymentGatewayService)
        {
            _orderRepository = orderRepository;
            _paymentGatewayService = paymentGatewayService;
        }

        public async Task<Result<CheckoutResultResponse>> Handle(CheckoutOrderCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _orderRepository.GetByIdWithLockAsync(request.OrderId, cancellationToken);
                if (order == null)
                    return Result<CheckoutResultResponse>.Failure("Không tìm thấy đơn hàng.");

                // 1. Domain xử lý chốt đơn (kiểm tra giỏ rỗng, đổi trạng thái)
                order.Checkout(request.PaymentMethodId);

                // 2. Gọi sang Module Payment lấy Link VNPay/Momo
                // (Nếu request.PaymentMethodId là Tiền mặt thì service bên kia tự hiểu và trả về URL null)
                var paymentResult = await _paymentGatewayService.GeneratePaymentUrlAsync(
                    order.Id,
                    order.OrderCode,
                    order.FinalTotal.Amount, // Tổng tiền từ ValueObject
                    request.PaymentMethodId,
                    cancellationToken
                );

                // 3. Trả kết quả kèm Link QR cho Frontend hiển thị ngay lập tức (Đáp ứng UI siêu mượt)
                var response = new CheckoutResultResponse(
                    order.Id,
                    paymentResult.PaymentUrl
                );

                return Result<CheckoutResultResponse>.Success(response);
            }
            catch (DomainException ex)
            {
                return Result<CheckoutResultResponse>.Failure(ex.Message);
            }
        }
    }
}
