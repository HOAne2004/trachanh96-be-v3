
using FluentValidation;
using MediatR;
using Orders.Application.Interfaces;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Orders.Application.Features.Commands
{
    public record CompleteOrderCommand(Guid OrderId): ICommand<Result<bool>>;

    public class CompleteOrderCommandValidator : AbstractValidator<CompleteOrderCommand>
    {
        public CompleteOrderCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
        }
    }

    public class CompleteOrderCommandHandler : IRequestHandler<CompleteOrderCommand, Result<bool>>
    {
        private readonly IOrderRepository _orderRepository;

        public CompleteOrderCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Result<bool>> Handle(CompleteOrderCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
                if (order == null)
                    return Result<bool>.Failure("Không tìm thấy đơn hàng.");

                order.Complete();

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
