using FluentValidation;
using MediatR;
using Orders.Application.Interfaces;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;


namespace Orders.Application.Features.Commands
{
    public record ShipOrderCommand(Guid OrderId, Guid StaffId) : ICommand<Result<bool>>;

    public class ShipOrderCommandValidator : AbstractValidator<ShipOrderCommand>
    {
        public ShipOrderCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.StaffId).NotEmpty().WithMessage("Không xác định được nhân viên thao tác.");
        }
    }

    public class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, Result<bool>>
    {
        private readonly IOrderRepository _orderRepository;
        public ShipOrderCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }
        public async Task<Result<bool>> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
                if (order == null) return Result<bool>.Failure("Không tìm thấy đơn hàng.");
                order.Ship(request.StaffId);
                return Result<bool>.Success(true);
            }
            catch (DomainException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }
    }
}
