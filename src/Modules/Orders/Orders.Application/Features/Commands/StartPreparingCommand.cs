using FluentValidation;
using MediatR;
using Orders.Application.Interfaces;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Orders.Application.Features.Commands
{
    public record StartPreparingCommand(Guid OrderId, Guid StaffId) : ICommand<Result<bool>>;
    public class StartPreparingCommandValidator : AbstractValidator<StartPreparingCommand>
    {
        public StartPreparingCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.StaffId).NotEmpty().WithMessage("Không xác định được nhân viên thao tác.");
        }
    }
    public class StartPreparingCommandHandler : IRequestHandler<StartPreparingCommand, Result<bool>>
    {
        private readonly IOrderRepository _orderRepository;

        public StartPreparingCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Result<bool>> Handle(StartPreparingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
                if (order == null) return Result<bool>.Failure("Không tìm thấy đơn hàng.");

                // Chuyển trạng thái từ Confirmed -> Preparing
                order.StartPreparing(request.StaffId);

                return Result<bool>.Success(true);
            }
            catch (DomainException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }
    }
}
