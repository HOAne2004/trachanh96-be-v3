using FluentValidation;
using MediatR;
using Orders.Application.Interfaces;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Orders.Application.Features.Commands;

// 1. COMMAND
public record ConfirmOrderCommand(
    Guid OrderId,
    Guid StaffId 
) : ICommand<Result<bool>>;

// 2. VALIDATOR
public class ConfirmOrderCommandValidator : AbstractValidator<ConfirmOrderCommand>
{
    public ConfirmOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.StaffId).NotEmpty().WithMessage("Không xác định được nhân viên thao tác.");
    }
}

// 3. HANDLER
public class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, Result<bool>>
{
    private readonly IOrderRepository _orderRepository;

    public ConfirmOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<bool>> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null) return Result<bool>.Failure("Không tìm thấy đơn hàng.");

            order.Confirm(request.StaffId);

            return Result<bool>.Success(true);
        }
        catch (DomainException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }
    }
}