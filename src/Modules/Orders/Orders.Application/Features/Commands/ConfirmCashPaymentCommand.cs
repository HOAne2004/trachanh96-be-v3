using FluentValidation;
using MediatR;
using Orders.Application.Interfaces;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Orders.Application.Features.Commands
{
    // ==========================================
    // 1. Command
    // ==========================================
    public record ConfirmCashPaymentCommand(
        Guid OrderId,
        Guid StaffId
    ) : ICommand<Result<bool>>;

    // ==========================================
    // 2. Validator
    // ==========================================
    public class ConfirmCashPaymentCommandValidator : AbstractValidator<ConfirmCashPaymentCommand>
    {
        public ConfirmCashPaymentCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.StaffId).NotEmpty().WithMessage("Không xác định được nhân viên thao tác.");
        }
    }

    // ==========================================
    // 3. Handler
    // ==========================================
    public class ConfirmCashPaymentCommandHandler : IRequestHandler<ConfirmCashPaymentCommand, Result<bool>>
    {
        private readonly IOrderRepository _orderRepository;

        public ConfirmCashPaymentCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Result<bool>> Handle(ConfirmCashPaymentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Sử dụng Lock vì thời điểm này khách hàng có thể đang thao tác hủy đơn trên điện thoại
                var order = await _orderRepository.GetByIdWithLockAsync(request.OrderId, cancellationToken);

                if (order == null)
                    return Result<bool>.Failure("Không tìm thấy đơn hàng.");

                // Sinh một TransactionId ảo để ghi nhận dòng tiền vào hệ thống
                // Cấu trúc: CASH_[StaffId]_[Ticks] để dễ dàng truy vết nhân viên nào đã thu khoản tiền này
                string manualTransactionId = $"CASH_{request.StaffId:N}_{DateTime.UtcNow.Ticks}";

                // Gọi Domain Logic để chuyển trạng thái sang Pending
                order.MarkAsPaid(manualTransactionId);

                return Result<bool>.Success(true);
            }
            catch (InvalidOperationException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
            catch (DomainException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }
    }
}