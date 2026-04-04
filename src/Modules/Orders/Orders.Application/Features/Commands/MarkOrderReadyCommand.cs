
using FluentValidation;
using MediatR;
using Orders.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;


namespace Orders.Application.Features.Commands
{
    public record MarkOrderReadyCommand(
        Guid OrderId,
        Guid StaffId) : IRequest<Result<bool>>;

    public class MarkOrderReadyCommandValidator : AbstractValidator<MarkOrderReadyCommand>
    {
        public MarkOrderReadyCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.StaffId).NotEmpty().WithMessage("Không xác định được nhân viên thao tác.");
        }
    }
    public class MarkOrderReadyCommandHandler : IRequestHandler<MarkOrderReadyCommand, Result<bool>>
    {
        private readonly IOrderRepository _orderRepository;

        public MarkOrderReadyCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Result<bool>> Handle(MarkOrderReadyCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
                if (order == null)
                    return Result<bool>.Failure("Không tìm thấy đơn hàng.");

                order.MarkAsReady(request.StaffId);

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
