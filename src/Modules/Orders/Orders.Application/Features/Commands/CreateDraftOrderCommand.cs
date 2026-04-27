using FluentValidation;
using MediatR;
using Orders.Application.Interfaces;
using Orders.Domain.Enums;
using Shared.Application.Interfaces;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Orders.Application.Features.Commands;

// 1. Command
public record CreateDraftOrderCommand(
    Guid StoreId,
    Guid? CustomerId,
    OrderTypeEnum OrderType,
    string Currency = "VND"
) : ICommand<Result<Guid>>;

// 2. Validator (Gatekeeper)
public class CreateDraftOrderCommandValidator : AbstractValidator<CreateDraftOrderCommand>
{
    public CreateDraftOrderCommandValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty().WithMessage("StoreId không được để trống.");
        RuleFor(x => x.OrderType).IsInEnum().WithMessage("Loại đơn hàng không hợp lệ.");
        
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Tiền tệ không được để trống.")
            .Length(3).WithMessage("Mã tiền tệ phải có đúng 3 ký tự (VD: VND, USD).");

        RuleFor(x => x)
            .Must(x => !(x.OrderType == OrderTypeEnum.Delivery && x.CustomerId == null))
            .WithMessage("Đơn giao hàng (Delivery) bắt buộc phải có thông tin CustomerId.");
    }
}

// 3. Handler
public class CreateDraftOrderCommandHandler : IRequestHandler<CreateDraftOrderCommand, Result<Guid>>
{
    private readonly IOrderRepository _orderRepository;

    public CreateDraftOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    } 

    public async Task<Result<Guid>> Handle(CreateDraftOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Tùy chọn Idempotency Logic Cấp cao (Business Check):
            // if (await _orderRepository.HasActiveDraftAsync(request.CustomerId, request.StoreId))
            //     return Result<Guid>.Failure("Bạn đang có một đơn hàng nháp chưa hoàn tất tại cửa hàng này.");

            // 1. Ủy quyền hoàn toàn cho Domain
            var order = Domain.Entities.Order.Create(
                request.StoreId,
                request.CustomerId,
                request.OrderType,
                request.Currency
            );

            // 2. Gọi AddAsync (Đồng bộ interface Repository theo chuẩn Async)
             _orderRepository.Add(order);

            return Result<Guid>.Success(order.Id);
        }
        catch (DomainException ex) // Bắt Exception chuẩn Semantic
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}