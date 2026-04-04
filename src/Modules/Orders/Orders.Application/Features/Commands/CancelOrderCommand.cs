
using FluentValidation;
using MediatR;
using Orders.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Orders.Application.Features.Commands
{
    public record CancelOrderCommand(
        Guid OrderId,
        string Reason,
        Guid? CancelledBy,
        bool IsStaffOverride = false // Cờ xác định ai đang gọi lệnh này (Controller sẽ tự map dựa vào Role của Token)}
         ) : IRequest<Result<bool>>;

    public class CancelOrderCommandVidator : AbstractValidator<CancelOrderCommand>
    {
        public CancelOrderCommandVidator ()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Bắt buộc phải có lý do hủy đơn")
                .MaximumLength(500).WithMessage("Lý do không vượt quá 500 ký tự");
        }
    }

    public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<bool>>
    {
        private readonly IOrderRepository _orderRespository;

        public CancelOrderCommandHandler(IOrderRepository orderRepository)
        {
            _orderRespository = orderRepository;
        }

        public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _orderRespository.GetByIdAsync(request.OrderId, cancellationToken);
                if (order == null)
                {
                    return Result<bool>.Failure("Không tìm thấy đơn hàng.");
                }
                order.Cancel(request.Reason, request.CancelledBy, request.IsStaffOverride);
                return Result<bool>.Success(true);
            }catch(DomainException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
            catch(InvalidOperationException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }
    }
}
